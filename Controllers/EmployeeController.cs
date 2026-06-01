using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthInsuranceManagement.Data;
using HealthInsuranceManagement.Models;
using HealthInsuranceManagement.Models.ViewModels;
using HealthInsuranceManagement.Services;

namespace HealthInsuranceManagement.Controllers
{
    /// <summary>
    /// Handles Employee module:
    ///  - Dashboard
    ///  - View / Update own details
    ///  - Change password
    ///  - Search policies
    ///  - Order (request) a policy
    ///  - View own requests & assigned policies
    /// </summary>
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notifications;
        private readonly IClaimUsageService _claimUsage;

        public EmployeeController(ApplicationDbContext db, INotificationService notifications, IClaimUsageService claimUsage)
        {
            _db = db;
            _notifications = notifications;
            _claimUsage = claimUsage;
        }

        // ── Auth guard ─────────────────────────────────────────────────
        private IActionResult? EmpGuard()
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Employee)
                return RedirectToAction("Login", "Auth");
            return null;
        }

        private int CurrentEmpId => HttpContext.Session.GetInt32("UserId") ?? 0;

        // ══════════════════════════════════════════════════════════════
        //  DASHBOARD
        // ══════════════════════════════════════════════════════════════

        public async Task<IActionResult> Dashboard()
        {
            var guard = EmpGuard(); if (guard != null) return guard;

            var empId = CurrentEmpId;
            var vm = new EmployeeDashboardViewModel
            {
                Employee = await _db.EmpRegisters.FindAsync(empId),
                AssignedPolicies = await _db.PolicyOnEmployees
                    .Include(pe => pe.Policy).ThenInclude(p => p!.Company)
                    .Where(pe => pe.EmpId == empId)
                    .ToListAsync(),
                MyRequests = await _db.PolicyRequestDetails
                    .Include(r => r.Policy)
                    .Include(r => r.Bill)
                    .Where(r => r.EmpId == empId)
                    .OrderByDescending(r => r.RequestedAt)
                    .ToListAsync(),
                RecentClaims = await _db.InsuranceClaims
                    .Include(c => c.AssignedPolicy).ThenInclude(pe => pe!.Policy)
                    .Where(c => c.EmpId == empId)
                    .OrderByDescending(c => c.SubmittedAt)
                    .Take(5)
                    .ToListAsync()
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> MyClaims()
        {
            var guard = EmpGuard(); if (guard != null) return guard;

            var vm = new EmployeeClaimsViewModel
            {
                PolicyUsage = await BuildClaimUsageAsync(CurrentEmpId),
                Claims = await _db.InsuranceClaims
                    .Include(c => c.AssignedPolicy).ThenInclude(pe => pe!.Policy).ThenInclude(p => p!.Company)
                    .Include(c => c.Manager)
                    .Include(c => c.FinanceManager)
                    .Where(c => c.EmpId == CurrentEmpId)
                    .OrderByDescending(c => c.SubmittedAt)
                    .ToListAsync()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> SubmitClaim(int? policyOnEmployeeId)
        {
            var guard = EmpGuard(); if (guard != null) return guard;

            var vm = new SubmitClaimViewModel
            {
                PolicyOnEmployeeId = policyOnEmployeeId ?? 0,
                PolicyUsage = await BuildClaimUsageAsync(CurrentEmpId)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitClaim(SubmitClaimViewModel model)
        {
            var guard = EmpGuard(); if (guard != null) return guard;

            var assignment = await _db.PolicyOnEmployees
                .Include(pe => pe.Employee)
                .Include(pe => pe.Policy).ThenInclude(p => p!.Company)
                .FirstOrDefaultAsync(pe => pe.Id == model.PolicyOnEmployeeId
                    && pe.EmpId == CurrentEmpId
                    && pe.Status == "Active");

            if (assignment == null)
                ModelState.AddModelError(nameof(model.PolicyOnEmployeeId), "Select one of your active assigned policies.");

            if (assignment != null && !await _claimUsage.HasAvailableCoverageAsync(assignment.Id, model.ClaimAmount))
            {
                var usage = await _claimUsage.GetUsageAsync(assignment.Id);
                ModelState.AddModelError(nameof(model.ClaimAmount), $"Claim exceeds remaining coverage. Available after pending claims: PKR {usage.AvailableAfterPending:N0}.");
            }

            if (!ModelState.IsValid)
            {
                model.PolicyUsage = await BuildClaimUsageAsync(CurrentEmpId);
                return View(model);
            }

            var claim = new InsuranceClaim
            {
                EmpId = CurrentEmpId,
                PolicyOnEmployeeId = assignment!.Id,
                ClaimType = model.ClaimType.Trim(),
                ClaimAmount = model.ClaimAmount,
                Reason = model.Reason.Trim(),
                Status = InsuranceClaim.Pending,
                SubmittedAt = DateTime.UtcNow
            };

            _db.InsuranceClaims.Add(claim);
            await _db.SaveChangesAsync();

            var employeeName = $"{assignment.Employee?.FirstName} {assignment.Employee?.LastName}".Trim();
            var body = $"""
                <p><strong>{employeeName}</strong> submitted a new insurance claim.</p>
                <p><strong>Policy:</strong> {assignment.Policy?.PolicyName}</p>
                <p><strong>Claim Type:</strong> {claim.ClaimType}</p>
                <p><strong>Claim Amount:</strong> PKR {claim.ClaimAmount:N0}</p>
                <p>This claim is waiting for manager review.</p>
                """;

            await _notifications.NotifyAdminsAsync("New insurance claim submitted", body, "InsuranceClaimSubmitted", claim.ClaimId);
            await _notifications.NotifyStaffByRoleAsync(UserRoles.Manager, "New claim waiting for manager review", body, "InsuranceClaimSubmitted", claim.ClaimId);
            if (assignment.Employee != null)
            {
                await _notifications.NotifyAsync(
                    assignment.Employee.Email,
                    employeeName,
                    UserRoles.Employee,
                    "Your insurance claim was submitted",
                    $"""
                    <p>Your claim for <strong>{assignment.Policy?.PolicyName}</strong> has been submitted.</p>
                    <p><strong>Claim Amount:</strong> PKR {claim.ClaimAmount:N0}</p>
                    <p>Status: Pending manager review.</p>
                    """,
                    "InsuranceClaimSubmitted",
                    claim.ClaimId);
            }
            await _notifications.NotifyCompanyAsync(
                assignment.Policy?.Company,
                "An employee submitted a claim for your company policy",
                $"""
                <p>An employee submitted a claim for a policy linked to your company.</p>
                <p><strong>Employee:</strong> {employeeName}</p>
                <p><strong>Policy:</strong> {assignment.Policy?.PolicyName}</p>
                <p><strong>Claim Amount:</strong> PKR {claim.ClaimAmount:N0}</p>
                <p><strong>Status:</strong> Pending manager review</p>
                """,
                "InsuranceClaimSubmitted",
                claim.ClaimId);

            TempData["Success"] = "Your claim has been submitted for manager review.";
            return RedirectToAction("MyClaims");
        }

        [HttpGet]
        public async Task<IActionResult> ClaimDetails(int id)
        {
            var guard = EmpGuard(); if (guard != null) return guard;

            var claim = await _db.InsuranceClaims
                .Include(c => c.AssignedPolicy).ThenInclude(pe => pe!.Policy).ThenInclude(p => p!.Company)
                .Include(c => c.Manager)
                .Include(c => c.FinanceManager)
                .FirstOrDefaultAsync(c => c.ClaimId == id && c.EmpId == CurrentEmpId);

            if (claim == null) return NotFound();
            ViewBag.Usage = await _claimUsage.GetUsageAsync(claim.PolicyOnEmployeeId);
            return View(claim);
        }

        // ══════════════════════════════════════════════════════════════
        //  MY DETAILS
        // ══════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> Details()
        {
            var guard = EmpGuard(); if (guard != null) return guard;
            var emp = await _db.EmpRegisters.FindAsync(CurrentEmpId);
            if (emp == null) return NotFound();
            return View(emp);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateDetails()
        {
            var guard = EmpGuard(); if (guard != null) return guard;
            var emp = await _db.EmpRegisters.FindAsync(CurrentEmpId);
            if (emp == null) return NotFound();
            return View(emp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDetails(EmpRegister model)
        {
            var guard = EmpGuard(); if (guard != null) return guard;
            ModelState.Remove("PasswordHash");
            ModelState.Remove("Username");
            if (!ModelState.IsValid) return View(model);

            var emp = await _db.EmpRegisters.FindAsync(CurrentEmpId);
            if (emp == null) return NotFound();

            emp.Phone       = model.Phone;
            emp.Address     = model.Address;
            emp.Email       = model.Email;
            emp.Designation = model.Designation;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Your details have been updated.";
            return RedirectToAction("Details");
        }

        // ══════════════════════════════════════════════════════════════
        //  CHANGE PASSWORD
        // ══════════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult ChangePassword()
        {
            var guard = EmpGuard(); if (guard != null) return guard;
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var guard = EmpGuard(); if (guard != null) return guard;
            if (!ModelState.IsValid) return View(model);

            var emp = await _db.EmpRegisters.FindAsync(CurrentEmpId);
            if (emp == null) return NotFound();

            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, emp.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                return View(model);
            }

            emp.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction("Dashboard");
        }

        // ══════════════════════════════════════════════════════════════
        //  SEARCH POLICIES
        // ══════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> SearchPolicy(string? policyType, decimal? minCoverage, decimal? maxPremium)
        {
            var guard = EmpGuard(); if (guard != null) return guard;

            minCoverage = minCoverage > 0 ? minCoverage : null;
            maxPremium = maxPremium > 0 ? maxPremium : null;

            var query = _db.Policies
                .Include(p => p.Company)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(policyType))
                query = query.Where(p => p.PolicyType == policyType);
            if (minCoverage.HasValue)
                query = query.Where(p => p.CoverageAmount >= minCoverage.Value);
            if (maxPremium.HasValue)
                query = query.Where(p => p.PremiumAmount <= maxPremium.Value);

            var vm = new PolicySearchViewModel
            {
                PolicyType  = policyType,
                MinCoverage = minCoverage,
                MaxPremium  = maxPremium,
                Results     = await query.ToListAsync()
            };
            return View(vm);
        }

        // View full details of a single policy
        public async Task<IActionResult> PolicyDetails(int id)
        {
            var guard = EmpGuard(); if (guard != null) return guard;
            var policy = await _db.Policies
                .Include(p => p.Company)
                .Include(p => p.PolicyDescriptions)
                .FirstOrDefaultAsync(p => p.PolicyId == id);
            if (policy == null) return NotFound();
            return View(policy);
        }

        // ══════════════════════════════════════════════════════════════
        //  ORDER INSURANCE  (Request a policy)
        // ══════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> OrderInsurance(int policyId)
        {
            var guard = EmpGuard(); if (guard != null) return guard;
            var policy = await _db.Policies.Include(p => p.Company).FirstOrDefaultAsync(p => p.PolicyId == policyId);
            if (policy == null) return NotFound();
            ViewBag.Policy = policy;
            return View(new PolicyRequestDetails { EmpId = CurrentEmpId, PolicyId = policyId, BillAmount = policy.PremiumAmount });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrderInsurance(PolicyRequestDetails model)
        {
            var guard = EmpGuard(); if (guard != null) return guard;

            // Check duplicate pending request
            var existing = await _db.PolicyRequestDetails
                .AnyAsync(r => r.EmpId == CurrentEmpId && r.PolicyId == model.PolicyId && r.Status == "Pending");
            if (existing)
            {
                TempData["Error"] = "You already have a pending request for this policy.";
                return RedirectToAction("SearchPolicy");
            }

            if (model.BillAmount <= 0)
            {
                var policy = await _db.Policies.Include(p => p.Company).FirstOrDefaultAsync(p => p.PolicyId == model.PolicyId);
                ViewBag.Policy = policy;
                ModelState.AddModelError("BillAmount", "Bill amount must be greater than zero.");
                return View(model);
            }

            model.EmpId       = CurrentEmpId;
            model.Status      = "Pending";
            model.RequestedAt = DateTime.UtcNow;
            _db.PolicyRequestDetails.Add(model);
            _db.PolicyBills.Add(new PolicyBill
            {
                PolicyRequest = model,
                Amount = model.BillAmount,
                Status = "Created",
                CreatedAt = model.RequestedAt
            });
            await _db.SaveChangesAsync();

            var request = await _db.PolicyRequestDetails
                .Include(r => r.Employee)
                .Include(r => r.Policy).ThenInclude(p => p!.Company)
                .FirstOrDefaultAsync(r => r.RequestId == model.RequestId);

            if (request?.Employee != null && request.Policy != null)
            {
                var employeeName = $"{request.Employee.FirstName} {request.Employee.LastName}";
                var body = $"""
                    <p><strong>{employeeName}</strong> submitted a new insurance request.</p>
                    <p><strong>Policy:</strong> {request.Policy.PolicyName}</p>
                    <p><strong>Bill Amount:</strong> PKR {request.BillAmount:N0}</p>
                    <p>This request is now waiting on the manager dashboard.</p>
                    """;

                await _notifications.NotifyAdminsAsync("New insurance request submitted", body, "PolicyRequestSubmitted", request.RequestId);
                await _notifications.NotifyStaffByRoleAsync(UserRoles.Manager, "New request waiting for manager review", body, "PolicyRequestSubmitted", request.RequestId);

                await _notifications.NotifyAsync(
                    request.Employee.Email,
                    employeeName,
                    UserRoles.Employee,
                    "Your insurance request was submitted",
                    $"""
                    <p>Your request for <strong>{request.Policy.PolicyName}</strong> has been submitted.</p>
                    <p><strong>Bill Amount:</strong> PKR {request.BillAmount:N0}</p>
                    <p>Status: Pending manager review.</p>
                    """,
                    "PolicyRequestSubmitted",
                    request.RequestId);
                await _notifications.NotifyCompanyAsync(
                    request.Policy.Company,
                    "An employee requested your company policy",
                    $"""
                    <p>An employee submitted a request for a policy linked to your company.</p>
                    <p><strong>Employee:</strong> {employeeName}</p>
                    <p><strong>Employee Email:</strong> {request.Employee.Email}</p>
                    <p><strong>Policy:</strong> {request.Policy.PolicyName}</p>
                    <p><strong>Company:</strong> {request.Policy.Company?.CompanyName ?? "N/A"}</p>
                    <p><strong>Bill Amount:</strong> PKR {request.BillAmount:N0}</p>
                    <p><strong>Status:</strong> Pending manager review</p>
                    """,
                    "PolicyRequestSubmitted",
                    request.RequestId);
            }

            TempData["Success"] = "Your insurance request and bill have been submitted. Awaiting manager approval.";
            return RedirectToAction("Dashboard");
        }

        private async Task<List<ClaimUsageViewModel>> BuildClaimUsageAsync(int empId)
        {
            var assignments = await _db.PolicyOnEmployees
                .Include(pe => pe.Policy).ThenInclude(p => p!.Company)
                .Where(pe => pe.EmpId == empId && pe.Status == "Active")
                .OrderBy(pe => pe.Policy!.PolicyName)
                .ToListAsync();

            var usageRows = new List<ClaimUsageViewModel>();
            foreach (var assignment in assignments)
            {
                var usage = await _claimUsage.GetUsageAsync(assignment.Id);
                usageRows.Add(new ClaimUsageViewModel
                {
                    PolicyOnEmployeeId = assignment.Id,
                    PolicyName = assignment.Policy?.PolicyName ?? "Assigned policy",
                    CompanyName = assignment.Policy?.Company?.CompanyName ?? "-",
                    Status = assignment.Status,
                    StartDate = assignment.StartDate,
                    EndDate = assignment.EndDate,
                    CoverageLimit = usage.CoverageLimit,
                    UsedAmount = usage.UsedAmount,
                    PendingAmount = usage.PendingAmount,
                    RemainingAmount = usage.RemainingAmount,
                    AvailableAfterPending = usage.AvailableAfterPending
                });
            }

            return usageRows;
        }
    }
}
