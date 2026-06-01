using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthInsuranceManagement.Data;
using HealthInsuranceManagement.Models;
using HealthInsuranceManagement.Models.ViewModels;
using HealthInsuranceManagement.Services;

namespace HealthInsuranceManagement.Controllers
{
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notifications;
        private readonly IClaimUsageService _claimUsage;

        public ManagerController(ApplicationDbContext db, INotificationService notifications, IClaimUsageService claimUsage)
        {
            _db = db;
            _notifications = notifications;
            _claimUsage = claimUsage;
        }

        private IActionResult? ManagerGuard()
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Manager)
                return RedirectToAction("Login", "Auth");
            return null;
        }

        private int CurrentManagerId => HttpContext.Session.GetInt32("UserId") ?? 0;

        public async Task<IActionResult> Dashboard()
        {
            var guard = ManagerGuard(); if (guard != null) return guard;
            await EnsureBillsForRequestsAsync();

            var vm = new ManagerDashboardViewModel
            {
                Manager = await _db.EmpRegisters.FindAsync(CurrentManagerId),
                ReceivedBills = await _db.PolicyBills
                    .Include(b => b.PolicyRequest).ThenInclude(r => r!.Employee)
                    .Include(b => b.PolicyRequest).ThenInclude(r => r!.Policy)
                    .ThenInclude(p => p!.Company)
                    .Where(b => b.Status == "Created" || b.Status == "ManagerApproved" || b.ManagerId == CurrentManagerId)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync(),
                PendingClaims = await _db.InsuranceClaims
                    .Include(c => c.Employee)
                    .Include(c => c.AssignedPolicy).ThenInclude(pe => pe!.Policy).ThenInclude(p => p!.Company)
                    .Where(c => c.Status == InsuranceClaim.Pending)
                    .OrderByDescending(c => c.SubmittedAt)
                    .Take(6)
                    .ToListAsync()
            };

            return View(vm);
        }

        public async Task<IActionResult> Requests()
        {
            var guard = ManagerGuard(); if (guard != null) return guard;
            await EnsureBillsForRequestsAsync();

            var bills = await _db.PolicyBills
                .Include(b => b.PolicyRequest).ThenInclude(r => r!.Employee)
                .Include(b => b.PolicyRequest).ThenInclude(r => r!.Policy)
                .ThenInclude(p => p!.Company)
                .Include(b => b.Manager)
                .Include(b => b.FinanceManager)
                .Where(b => b.Status == "Created" || b.Status == "ManagerApproved" || b.ManagerId == CurrentManagerId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bills);
        }

        public async Task<IActionResult> RequestDetails(int id)
        {
            var guard = ManagerGuard(); if (guard != null) return guard;
            await EnsureBillsForRequestsAsync();

            var bill = await _db.PolicyBills
                .Include(b => b.PolicyRequest).ThenInclude(r => r!.Employee)
                .Include(b => b.PolicyRequest).ThenInclude(r => r!.Policy)
                .ThenInclude(p => p!.Company)
                .Include(b => b.PolicyRequest).ThenInclude(r => r!.Approval)
                .ThenInclude(a => a!.Manager)
                .Include(b => b.Manager)
                .Include(b => b.FinanceManager)
                .FirstOrDefaultAsync(b => b.RequestId == id
                    && (b.Status == "Created" || b.Status == "ManagerApproved" || b.ManagerId == CurrentManagerId));

            if (bill?.PolicyRequest == null) return NotFound();
            return View(bill);
        }

        public async Task<IActionResult> Claims()
        {
            var guard = ManagerGuard(); if (guard != null) return guard;

            var claims = await _db.InsuranceClaims
                .Include(c => c.Employee)
                .Include(c => c.AssignedPolicy).ThenInclude(pe => pe!.Policy).ThenInclude(p => p!.Company)
                .Include(c => c.Manager)
                .OrderBy(c => c.Status == InsuranceClaim.Pending ? 0 : 1)
                .ThenByDescending(c => c.SubmittedAt)
                .ToListAsync();

            return View(claims);
        }

        public async Task<IActionResult> ClaimDetails(int id)
        {
            var guard = ManagerGuard(); if (guard != null) return guard;

            var claim = await _db.InsuranceClaims
                .Include(c => c.Employee)
                .Include(c => c.AssignedPolicy).ThenInclude(pe => pe!.Policy).ThenInclude(p => p!.Company)
                .Include(c => c.Manager)
                .Include(c => c.FinanceManager)
                .FirstOrDefaultAsync(c => c.ClaimId == id);

            if (claim == null) return NotFound();
            ViewBag.Usage = await _claimUsage.GetUsageAsync(claim.PolicyOnEmployeeId, claim.ClaimId);
            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecideClaim(int claimId, string decision, string? remarks)
        {
            var guard = ManagerGuard(); if (guard != null) return guard;

            var claim = await _db.InsuranceClaims
                .Include(c => c.Employee)
                .Include(c => c.AssignedPolicy).ThenInclude(pe => pe!.Policy).ThenInclude(p => p!.Company)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null) return NotFound();
            if (claim.Status != InsuranceClaim.Pending)
            {
                TempData["Error"] = "Only pending claims can be approved or rejected.";
                return RedirectToAction("Claims");
            }

            var approved = decision == InsuranceClaim.Approved;
            if (!approved && string.IsNullOrWhiteSpace(remarks))
            {
                TempData["Error"] = "Remarks are required when rejecting a claim.";
                return RedirectToAction("ClaimDetails", new { id = claimId });
            }

            if (approved && !await _claimUsage.HasAvailableCoverageAsync(claim.PolicyOnEmployeeId, claim.ClaimAmount, claim.ClaimId))
            {
                var usage = await _claimUsage.GetUsageAsync(claim.PolicyOnEmployeeId, claim.ClaimId);
                TempData["Error"] = $"Claim exceeds remaining coverage. Available after pending claims: PKR {usage.AvailableAfterPending:N0}.";
                return RedirectToAction("ClaimDetails", new { id = claimId });
            }

            claim.Status = approved ? InsuranceClaim.Approved : InsuranceClaim.Rejected;
            claim.ApprovedAmount = approved ? claim.ClaimAmount : 0;
            claim.ManagerId = CurrentManagerId;
            claim.ReviewedAt = DateTime.UtcNow;
            claim.Remarks = remarks;

            await _db.SaveChangesAsync();

            var employeeName = $"{claim.Employee?.FirstName} {claim.Employee?.LastName}".Trim();
            var decisionText = approved ? "approved" : "rejected";
            var body = $"""
                <p>A manager has <strong>{decisionText}</strong> an insurance claim.</p>
                <p><strong>Employee:</strong> {employeeName}</p>
                <p><strong>Policy:</strong> {claim.AssignedPolicy?.Policy?.PolicyName}</p>
                <p><strong>Claim Amount:</strong> PKR {claim.ClaimAmount:N0}</p>
                <p><strong>Remarks:</strong> {remarks ?? "No remarks provided"}</p>
                """;

            await _notifications.NotifyAdminsAsync($"Insurance claim {decisionText}", body, "InsuranceClaimDecision", claim.ClaimId);
            if (approved)
                await _notifications.NotifyStaffByRoleAsync(UserRoles.FinanceManager, "Approved claim waiting for finance", body, "InsuranceClaimDecision", claim.ClaimId);
            if (claim.Employee != null)
            {
                await _notifications.NotifyAsync(
                    claim.Employee.Email,
                    employeeName,
                    UserRoles.Employee,
                    $"Your insurance claim was {decisionText}",
                    $"""
                    <p>Your claim for <strong>{claim.AssignedPolicy?.Policy?.PolicyName}</strong> was <strong>{decisionText}</strong>.</p>
                    <p><strong>Claim Amount:</strong> PKR {claim.ClaimAmount:N0}</p>
                    <p><strong>Remarks:</strong> {remarks ?? "No remarks provided"}</p>
                    """,
                    "InsuranceClaimDecision",
                    claim.ClaimId);
            }
            await _notifications.NotifyCompanyAsync(
                claim.AssignedPolicy?.Policy?.Company,
                $"Insurance claim {decisionText} for your company policy",
                body,
                "InsuranceClaimDecision",
                claim.ClaimId);

            TempData["Success"] = approved ? "Claim approved and sent to finance." : "Claim rejected.";
            return RedirectToAction("Claims");
        }

        public async Task<IActionResult> Details()
        {
            var guard = ManagerGuard(); if (guard != null) return guard;
            var manager = await _db.EmpRegisters.FindAsync(CurrentManagerId);
            if (manager == null) return NotFound();
            return View("~/Views/Employee/Details.cshtml", manager);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateDetails()
        {
            var guard = ManagerGuard(); if (guard != null) return guard;
            var manager = await _db.EmpRegisters.FindAsync(CurrentManagerId);
            if (manager == null) return NotFound();
            return View("~/Views/Employee/UpdateDetails.cshtml", manager);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDetails(EmpRegister model)
        {
            var guard = ManagerGuard(); if (guard != null) return guard;
            ModelState.Remove("PasswordHash");
            ModelState.Remove("Username");

            if (!ModelState.IsValid)
                return View("~/Views/Employee/UpdateDetails.cshtml", model);

            var manager = await _db.EmpRegisters.FindAsync(CurrentManagerId);
            if (manager == null) return NotFound();

            manager.Phone = model.Phone;
            manager.Address = model.Address;
            manager.Email = model.Email;
            manager.Designation = model.Designation;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Your details have been updated.";
            return RedirectToAction("Details");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decide(int billId, string decision, string? remarks)
        {
            var guard = ManagerGuard(); if (guard != null) return guard;

            var bill = await _db.PolicyBills
                .Include(b => b.PolicyRequest)
                .FirstOrDefaultAsync(b => b.BillId == billId);
            if (bill?.PolicyRequest == null) return NotFound();
            if (bill.Status != "Created")
            {
                TempData["Error"] = "Only newly created bills can be approved or rejected.";
                return RedirectToAction("Dashboard");
            }

            var approved = decision == "Approved";
            bill.Status = approved ? "ManagerApproved" : "Rejected";
            bill.ManagerId = CurrentManagerId;
            bill.ManagerDecisionAt = DateTime.UtcNow;
            bill.Remarks = remarks;
            bill.PolicyRequest.Status = approved ? "ApprovedByManager" : "Rejected";

            _db.PolicyApprovalDetails.Add(new PolicyApprovalDetails
            {
                RequestId = bill.RequestId,
                ManagerId = CurrentManagerId,
                Decision = approved ? "Approved" : "Rejected",
                Remarks = remarks,
                DecisionDate = DateTime.UtcNow
            });

            if (approved)
            {
                await AssignPolicyToEmployeeAsync(bill.PolicyRequest);
            }

            await _db.SaveChangesAsync();

            var updatedBill = await _db.PolicyBills
                .Include(b => b.PolicyRequest).ThenInclude(r => r!.Employee)
                .Include(b => b.PolicyRequest).ThenInclude(r => r!.Policy).ThenInclude(p => p!.Company)
                .FirstOrDefaultAsync(b => b.BillId == billId);

            if (updatedBill?.PolicyRequest?.Employee != null && updatedBill.PolicyRequest.Policy != null)
            {
                var request = updatedBill.PolicyRequest;
                var employee = request.Employee;
                var employeeName = $"{employee.FirstName} {employee.LastName}";
                var decisionText = approved ? "approved" : "rejected";
                var body = $"""
                    <p>The manager has <strong>{decisionText}</strong> an insurance request.</p>
                    <p><strong>Employee:</strong> {employeeName}</p>
                    <p><strong>Policy:</strong> {request.Policy.PolicyName}</p>
                    <p><strong>Bill Amount:</strong> PKR {request.BillAmount:N0}</p>
                    """;

                await _notifications.NotifyAdminsAsync($"Manager {decisionText} a policy request", body, "ManagerDecision", request.RequestId);
                await _notifications.NotifyAsync(
                    employee.Email,
                    employeeName,
                    UserRoles.Employee,
                    $"Your insurance request was {decisionText}",
                    $"""
                    <p>Your request for <strong>{request.Policy.PolicyName}</strong> was <strong>{decisionText}</strong> by the manager.</p>
                    <p><strong>Bill Amount:</strong> PKR {request.BillAmount:N0}</p>
                    """,
                    "ManagerDecision",
                    request.RequestId);
                await _notifications.NotifyCompanyAsync(
                    request.Policy.Company,
                    $"Manager {decisionText} a request for your company policy",
                    $"""
                    <p>A manager reviewed a request for a policy linked to your company.</p>
                    <p><strong>Decision:</strong> {decisionText}</p>
                    <p><strong>Employee:</strong> {employeeName}</p>
                    <p><strong>Employee Email:</strong> {employee.Email}</p>
                    <p><strong>Policy:</strong> {request.Policy.PolicyName}</p>
                    <p><strong>Bill Amount:</strong> PKR {request.BillAmount:N0}</p>
                    <p><strong>Remarks:</strong> {remarks ?? "No remarks provided"}</p>
                    """,
                    "ManagerDecision",
                    request.RequestId);
            }

            TempData["Success"] = approved ? "Bill approved by manager." : "Bill rejected.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForwardToFinance(int billId)
        {
            var guard = ManagerGuard(); if (guard != null) return guard;

            var bill = await _db.PolicyBills
                .Include(b => b.PolicyRequest)
                .FirstOrDefaultAsync(b => b.BillId == billId);
            if (bill?.PolicyRequest == null) return NotFound();
            if (bill.Status != "ManagerApproved")
            {
                TempData["Error"] = "Approve the bill before forwarding it to finance.";
                return RedirectToAction("Dashboard");
            }

            bill.Status = "ForwardedToFinance";
            bill.ForwardedAt = DateTime.UtcNow;
            bill.PolicyRequest.Status = "ForwardedToFinance";
            await _db.SaveChangesAsync();

            var forwardedBill = await _db.PolicyBills
                .Include(b => b.PolicyRequest).ThenInclude(r => r!.Employee)
                .Include(b => b.PolicyRequest).ThenInclude(r => r!.Policy).ThenInclude(p => p!.Company)
                .FirstOrDefaultAsync(b => b.BillId == billId);

            if (forwardedBill?.PolicyRequest?.Employee != null && forwardedBill.PolicyRequest.Policy != null)
            {
                var request = forwardedBill.PolicyRequest;
                var employee = request.Employee;
                var employeeName = $"{employee.FirstName} {employee.LastName}";
                var body = $"""
                    <p>A manager-approved bill was forwarded to finance.</p>
                    <p><strong>Employee:</strong> {employeeName}</p>
                    <p><strong>Policy:</strong> {request.Policy.PolicyName}</p>
                    <p><strong>Bill Amount:</strong> PKR {forwardedBill.Amount:N0}</p>
                    """;

                await _notifications.NotifyAdminsAsync("Bill forwarded to finance", body, "ForwardedToFinance", request.RequestId);
                await _notifications.NotifyStaffByRoleAsync(UserRoles.FinanceManager, "New bill waiting for finance", body, "ForwardedToFinance", request.RequestId);
                await _notifications.NotifyAsync(
                    employee.Email,
                    employeeName,
                    UserRoles.Employee,
                    "Your bill was forwarded to finance",
                    $"""
                    <p>Your request for <strong>{request.Policy.PolicyName}</strong> has been forwarded to finance.</p>
                    <p><strong>Bill Amount:</strong> PKR {forwardedBill.Amount:N0}</p>
                    """,
                    "ForwardedToFinance",
                    request.RequestId);
                await _notifications.NotifyCompanyAsync(
                    request.Policy.Company,
                    "A request for your policy was forwarded to finance",
                    $"""
                    <p>A manager-approved bill for your company policy was forwarded to finance.</p>
                    <p><strong>Employee:</strong> {employeeName}</p>
                    <p><strong>Employee Email:</strong> {employee.Email}</p>
                    <p><strong>Policy:</strong> {request.Policy.PolicyName}</p>
                    <p><strong>Bill Amount:</strong> PKR {forwardedBill.Amount:N0}</p>
                    <p><strong>Status:</strong> Forwarded to finance</p>
                    """,
                    "ForwardedToFinance",
                    request.RequestId);
            }

            TempData["Success"] = "Bill forwarded to Finance Manager.";
            return RedirectToAction("Dashboard");
        }

        private async Task AssignPolicyToEmployeeAsync(PolicyRequestDetails request)
        {
            var alreadyAssigned = await _db.PolicyOnEmployees
                .AnyAsync(pe => pe.EmpId == request.EmpId && pe.PolicyId == request.PolicyId && pe.Status == "Active");
            if (alreadyAssigned) return;

            var policy = await _db.Policies.FindAsync(request.PolicyId);
            if (policy == null) return;

            _db.PolicyOnEmployees.Add(new PolicyOnEmployee
            {
                EmpId = request.EmpId,
                PolicyId = request.PolicyId,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(policy.DurationMonths),
                ClaimedAmount = request.BillAmount,
                Status = "Active"
            });
        }

        private async Task EnsureBillsForRequestsAsync()
        {
            var requestsWithoutBills = await _db.PolicyRequestDetails
                .Include(r => r.Bill)
                .Where(r => r.Bill == null)
                .ToListAsync();

            if (!requestsWithoutBills.Any()) return;

            foreach (var request in requestsWithoutBills)
            {
                _db.PolicyBills.Add(new PolicyBill
                {
                    RequestId = request.RequestId,
                    Amount = request.BillAmount,
                    Status = GetBillStatusFromRequest(request.Status),
                    CreatedAt = request.RequestedAt
                });
            }

            await _db.SaveChangesAsync();
        }

        private static string GetBillStatusFromRequest(string requestStatus)
        {
            return requestStatus switch
            {
                "Rejected" => "Rejected",
                "ApprovedByManager" => "ManagerApproved",
                "ManagerApproved" => "ManagerApproved",
                "ForwardedToFinance" => "ForwardedToFinance",
                "Paid" => "Paid",
                "Closed" => "Closed",
                _ => "Created"
            };
        }
    }
}
