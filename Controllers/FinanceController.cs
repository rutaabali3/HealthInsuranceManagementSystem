using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthInsuranceManagement.Data;
using HealthInsuranceManagement.Models;
using HealthInsuranceManagement.Models.ViewModels;
using HealthInsuranceManagement.Services;

namespace HealthInsuranceManagement.Controllers
{
    public class FinanceController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notifications;

        public FinanceController(ApplicationDbContext db, INotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        private IActionResult? FinanceGuard()
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.FinanceManager)
                return RedirectToAction("Login", "Auth");
            return null;
        }

        private int CurrentFinanceManagerId => HttpContext.Session.GetInt32("UserId") ?? 0;

        public async Task<IActionResult> Dashboard()
        {
            var guard = FinanceGuard(); if (guard != null) return guard;
            await EnsureBillsForRequestsAsync();

            var vm = new FinanceDashboardViewModel
            {
                FinanceManager = await _db.EmpRegisters.FindAsync(CurrentFinanceManagerId),
                ReceivedBills = await _db.PolicyBills
                    .Include(b => b.PolicyRequest).ThenInclude(r => r!.Employee)
                    .Include(b => b.PolicyRequest).ThenInclude(r => r!.Policy)
                    .ThenInclude(p => p!.Company)
                    .Include(b => b.Manager)
                    .Where(b => b.Status != "Rejected" || b.FinanceManagerId == CurrentFinanceManagerId)
                    .OrderByDescending(b => b.ForwardedAt ?? b.CreatedAt)
                    .ToListAsync(),
                ReceivedClaims = await _db.InsuranceClaims
                    .Include(c => c.Employee)
                    .Include(c => c.AssignedPolicy).ThenInclude(pe => pe!.Policy).ThenInclude(p => p!.Company)
                    .Include(c => c.Manager)
                    .Where(c => c.Status == InsuranceClaim.Approved || c.Status == InsuranceClaim.Paid || c.Status == InsuranceClaim.Closed)
                    .OrderByDescending(c => c.ReviewedAt ?? c.SubmittedAt)
                    .Take(6)
                    .ToListAsync()
            };

            return View(vm);
        }

        public async Task<IActionResult> Requests()
        {
            var guard = FinanceGuard(); if (guard != null) return guard;
            await EnsureBillsForRequestsAsync();

            var bills = await _db.PolicyBills
                .Include(b => b.PolicyRequest).ThenInclude(r => r!.Employee)
                .Include(b => b.PolicyRequest).ThenInclude(r => r!.Policy)
                .ThenInclude(p => p!.Company)
                .Include(b => b.Manager)
                .Include(b => b.FinanceManager)
                .Where(b => b.Status != "Rejected" || b.FinanceManagerId == CurrentFinanceManagerId)
                .OrderByDescending(b => b.ForwardedAt ?? b.CreatedAt)
                .ToListAsync();

            return View(bills);
        }

        public async Task<IActionResult> RequestDetails(int id)
        {
            var guard = FinanceGuard(); if (guard != null) return guard;
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
                    && (b.Status != "Rejected" || b.FinanceManagerId == CurrentFinanceManagerId));

            if (bill?.PolicyRequest == null) return NotFound();
            return View(bill);
        }

        public async Task<IActionResult> Claims()
        {
            var guard = FinanceGuard(); if (guard != null) return guard;

            var claims = await _db.InsuranceClaims
                .Include(c => c.Employee)
                .Include(c => c.AssignedPolicy).ThenInclude(pe => pe!.Policy).ThenInclude(p => p!.Company)
                .Include(c => c.Manager)
                .Include(c => c.FinanceManager)
                .Where(c => c.Status == InsuranceClaim.Approved || c.Status == InsuranceClaim.Paid || c.Status == InsuranceClaim.Closed)
                .OrderBy(c => c.Status == InsuranceClaim.Approved ? 0 : c.Status == InsuranceClaim.Paid ? 1 : 2)
                .ThenByDescending(c => c.ReviewedAt ?? c.SubmittedAt)
                .ToListAsync();

            return View(claims);
        }

        public async Task<IActionResult> ClaimDetails(int id)
        {
            var guard = FinanceGuard(); if (guard != null) return guard;

            var claim = await _db.InsuranceClaims
                .Include(c => c.Employee)
                .Include(c => c.AssignedPolicy).ThenInclude(pe => pe!.Policy).ThenInclude(p => p!.Company)
                .Include(c => c.Manager)
                .Include(c => c.FinanceManager)
                .FirstOrDefaultAsync(c => c.ClaimId == id
                    && (c.Status == InsuranceClaim.Approved || c.Status == InsuranceClaim.Paid || c.Status == InsuranceClaim.Closed));

            if (claim == null) return NotFound();
            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayClaim(int claimId)
        {
            var guard = FinanceGuard(); if (guard != null) return guard;

            var claim = await _db.InsuranceClaims
                .Include(c => c.Employee)
                .Include(c => c.AssignedPolicy).ThenInclude(pe => pe!.Policy).ThenInclude(p => p!.Company)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null) return NotFound();
            if (claim.Status != InsuranceClaim.Approved)
            {
                TempData["Error"] = "Only approved claims can be paid.";
                return RedirectToAction("Claims");
            }

            claim.Status = InsuranceClaim.Paid;
            claim.FinanceManagerId = CurrentFinanceManagerId;
            claim.PaidAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var employeeName = $"{claim.Employee?.FirstName} {claim.Employee?.LastName}".Trim();
            var body = $"""
                <p>Finance paid an approved insurance claim.</p>
                <p><strong>Employee:</strong> {employeeName}</p>
                <p><strong>Policy:</strong> {claim.AssignedPolicy?.Policy?.PolicyName}</p>
                <p><strong>Approved Amount:</strong> PKR {claim.ApprovedAmount:N0}</p>
                """;

            await _notifications.NotifyAdminsAsync("Insurance claim paid", body, "InsuranceClaimPaid", claim.ClaimId);
            if (claim.Employee != null)
            {
                await _notifications.NotifyAsync(
                    claim.Employee.Email,
                    employeeName,
                    UserRoles.Employee,
                    "Your insurance claim was paid",
                    $"""
                    <p>Finance has paid your claim for <strong>{claim.AssignedPolicy?.Policy?.PolicyName}</strong>.</p>
                    <p><strong>Approved Amount:</strong> PKR {claim.ApprovedAmount:N0}</p>
                    """,
                    "InsuranceClaimPaid",
                    claim.ClaimId);
            }
            await _notifications.NotifyCompanyAsync(
                claim.AssignedPolicy?.Policy?.Company,
                "Insurance claim paid for your company policy",
                body,
                "InsuranceClaimPaid",
                claim.ClaimId);

            TempData["Success"] = $"Claim payment of PKR {claim.ApprovedAmount:N0} credited.";
            return RedirectToAction("Claims");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseClaim(int claimId)
        {
            var guard = FinanceGuard(); if (guard != null) return guard;

            var claim = await _db.InsuranceClaims
                .Include(c => c.Employee)
                .Include(c => c.AssignedPolicy).ThenInclude(pe => pe!.Policy).ThenInclude(p => p!.Company)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null) return NotFound();
            if (claim.Status != InsuranceClaim.Paid)
            {
                TempData["Error"] = "Pay the claim before closing it.";
                return RedirectToAction("Claims");
            }

            claim.Status = InsuranceClaim.Closed;
            claim.ClosedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var employeeName = $"{claim.Employee?.FirstName} {claim.Employee?.LastName}".Trim();
            var body = $"""
                <p>A paid insurance claim was closed.</p>
                <p><strong>Employee:</strong> {employeeName}</p>
                <p><strong>Policy:</strong> {claim.AssignedPolicy?.Policy?.PolicyName}</p>
                <p><strong>Approved Amount:</strong> PKR {claim.ApprovedAmount:N0}</p>
                """;

            await _notifications.NotifyAdminsAsync("Insurance claim closed", body, "InsuranceClaimClosed", claim.ClaimId);
            if (claim.Employee != null)
            {
                await _notifications.NotifyAsync(
                    claim.Employee.Email,
                    employeeName,
                    UserRoles.Employee,
                    "Your insurance claim was closed",
                    $"""
                    <p>Your paid claim for <strong>{claim.AssignedPolicy?.Policy?.PolicyName}</strong> has been closed.</p>
                    <p><strong>Approved Amount:</strong> PKR {claim.ApprovedAmount:N0}</p>
                    """,
                    "InsuranceClaimClosed",
                    claim.ClaimId);
            }
            await _notifications.NotifyCompanyAsync(
                claim.AssignedPolicy?.Policy?.Company,
                "Insurance claim closed for your company policy",
                body,
                "InsuranceClaimClosed",
                claim.ClaimId);

            TempData["Success"] = "Claim closed after payment.";
            return RedirectToAction("Claims");
        }

        public async Task<IActionResult> Details()
        {
            var guard = FinanceGuard(); if (guard != null) return guard;
            var financeManager = await _db.EmpRegisters.FindAsync(CurrentFinanceManagerId);
            if (financeManager == null) return NotFound();
            return View("~/Views/Employee/Details.cshtml", financeManager);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateDetails()
        {
            var guard = FinanceGuard(); if (guard != null) return guard;
            var financeManager = await _db.EmpRegisters.FindAsync(CurrentFinanceManagerId);
            if (financeManager == null) return NotFound();
            return View("~/Views/Employee/UpdateDetails.cshtml", financeManager);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDetails(EmpRegister model)
        {
            var guard = FinanceGuard(); if (guard != null) return guard;
            ModelState.Remove("PasswordHash");
            ModelState.Remove("Username");

            if (!ModelState.IsValid)
                return View("~/Views/Employee/UpdateDetails.cshtml", model);

            var financeManager = await _db.EmpRegisters.FindAsync(CurrentFinanceManagerId);
            if (financeManager == null) return NotFound();

            financeManager.Phone = model.Phone;
            financeManager.Address = model.Address;
            financeManager.Email = model.Email;
            financeManager.Designation = model.Designation;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Your details have been updated.";
            return RedirectToAction("Details");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreditPayment(int billId)
        {
            var guard = FinanceGuard(); if (guard != null) return guard;

            var bill = await _db.PolicyBills
                .Include(b => b.PolicyRequest)
                .FirstOrDefaultAsync(b => b.BillId == billId);
            if (bill?.PolicyRequest == null) return NotFound();
            if (bill.Status != "ForwardedToFinance")
            {
                TempData["Error"] = "Only forwarded bills can be paid.";
                return RedirectToAction("Dashboard");
            }

            bill.Status = "Paid";
            bill.FinanceManagerId = CurrentFinanceManagerId;
            bill.PaidAt = DateTime.UtcNow;
            bill.PolicyRequest.Status = "Paid";
            await _db.SaveChangesAsync();

            var paidBill = await _db.PolicyBills
                .Include(b => b.PolicyRequest).ThenInclude(r => r!.Employee)
                .Include(b => b.PolicyRequest).ThenInclude(r => r!.Policy).ThenInclude(p => p!.Company)
                .FirstOrDefaultAsync(b => b.BillId == billId);

            if (paidBill?.PolicyRequest?.Employee != null && paidBill.PolicyRequest.Policy != null)
            {
                var request = paidBill.PolicyRequest;
                var employee = request.Employee;
                var employeeName = $"{employee.FirstName} {employee.LastName}";
                var body = $"""
                    <p>Finance credited payment for a policy request.</p>
                    <p><strong>Employee:</strong> {employeeName}</p>
                    <p><strong>Policy:</strong> {request.Policy.PolicyName}</p>
                    <p><strong>Amount:</strong> PKR {paidBill.Amount:N0}</p>
                    """;

                await _notifications.NotifyAdminsAsync("Finance credited a policy payment", body, "PaymentCredited", request.RequestId);
                await _notifications.NotifyAsync(
                    employee.Email,
                    employeeName,
                    UserRoles.Employee,
                    "Your policy payment was credited",
                    $"""
                    <p>Finance has credited payment for <strong>{request.Policy.PolicyName}</strong>.</p>
                    <p><strong>Amount:</strong> PKR {paidBill.Amount:N0}</p>
                    """,
                    "PaymentCredited",
                    request.RequestId);
                await _notifications.NotifyCompanyAsync(
                    request.Policy.Company,
                    "Payment was credited for your company policy",
                    $"""
                    <p>Finance credited payment for a request linked to your company policy.</p>
                    <p><strong>Employee:</strong> {employeeName}</p>
                    <p><strong>Employee Email:</strong> {employee.Email}</p>
                    <p><strong>Policy:</strong> {request.Policy.PolicyName}</p>
                    <p><strong>Amount:</strong> PKR {paidBill.Amount:N0}</p>
                    <p><strong>Status:</strong> Paid</p>
                    """,
                    "PaymentCredited",
                    request.RequestId);
            }

            TempData["Success"] = $"Payment of PKR {bill.Amount:N0} credited to the employee.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseRequest(int billId)
        {
            var guard = FinanceGuard(); if (guard != null) return guard;

            var bill = await _db.PolicyBills
                .Include(b => b.PolicyRequest)
                .FirstOrDefaultAsync(b => b.BillId == billId);
            if (bill?.PolicyRequest == null) return NotFound();
            if (bill.Status != "Paid")
            {
                TempData["Error"] = "Credit payment before closing the request.";
                return RedirectToAction("Dashboard");
            }

            bill.Status = "Closed";
            bill.ClosedAt = DateTime.UtcNow;
            bill.PolicyRequest.Status = "Closed";
            await _db.SaveChangesAsync();

            var closedBill = await _db.PolicyBills
                .Include(b => b.PolicyRequest).ThenInclude(r => r!.Employee)
                .Include(b => b.PolicyRequest).ThenInclude(r => r!.Policy).ThenInclude(p => p!.Company)
                .FirstOrDefaultAsync(b => b.BillId == billId);

            if (closedBill?.PolicyRequest?.Employee != null && closedBill.PolicyRequest.Policy != null)
            {
                var request = closedBill.PolicyRequest;
                var employee = request.Employee;
                var employeeName = $"{employee.FirstName} {employee.LastName}";
                var body = $"""
                    <p>A policy request was closed after payment.</p>
                    <p><strong>Employee:</strong> {employeeName}</p>
                    <p><strong>Policy:</strong> {request.Policy.PolicyName}</p>
                    <p><strong>Amount:</strong> PKR {closedBill.Amount:N0}</p>
                    """;

                await _notifications.NotifyAdminsAsync("Policy request closed", body, "RequestClosed", request.RequestId);
                await _notifications.NotifyAsync(
                    employee.Email,
                    employeeName,
                    UserRoles.Employee,
                    "Your policy request was closed",
                    $"""
                    <p>Your request for <strong>{request.Policy.PolicyName}</strong> has been closed after payment.</p>
                    <p><strong>Amount:</strong> PKR {closedBill.Amount:N0}</p>
                    """,
                    "RequestClosed",
                    request.RequestId);
                await _notifications.NotifyCompanyAsync(
                    request.Policy.Company,
                    "A request for your company policy was closed",
                    $"""
                    <p>A paid request linked to your company policy has been closed.</p>
                    <p><strong>Employee:</strong> {employeeName}</p>
                    <p><strong>Employee Email:</strong> {employee.Email}</p>
                    <p><strong>Policy:</strong> {request.Policy.PolicyName}</p>
                    <p><strong>Amount:</strong> PKR {closedBill.Amount:N0}</p>
                    <p><strong>Status:</strong> Closed</p>
                    """,
                    "RequestClosed",
                    request.RequestId);
            }

            TempData["Success"] = "Request closed after payment.";
            return RedirectToAction("Dashboard");
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
