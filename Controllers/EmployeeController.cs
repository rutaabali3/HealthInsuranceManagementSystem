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

        public EmployeeController(ApplicationDbContext db, INotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
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
                    .ToListAsync()
            };
            return View(vm);
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
    }
}
