using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthInsuranceManagement.Data;
using HealthInsuranceManagement.Models;
using HealthInsuranceManagement.Models.ViewModels;
using HealthInsuranceManagement.Services;

namespace HealthInsuranceManagement.Controllers
{
    /// <summary>
    /// Handles all Admin module operations:
    ///  - Dashboard
    ///  - Add/Edit/Delete Insurance Companies (Add Resource)
    ///  - Add/Edit Policies
    ///  - Register / Manage Employees (Employee Support)
    ///  - View and Approve/Reject Policy Requests
    /// </summary>
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notifications;

        public AdminController(ApplicationDbContext db, INotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        // ── Auth guard helper ──────────────────────────────────────────
        private IActionResult? AdminGuard()
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
                return RedirectToAction("Login", "Auth");
            return null;
        }

        // ══════════════════════════════════════════════════════════════
        //  DASHBOARD
        // ══════════════════════════════════════════════════════════════

        public async Task<IActionResult> Dashboard()
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var nextMonth = monthStart.AddMonths(1);
            var employeeDates = await _db.EmpRegisters
                .Where(e => e.RegisteredAt >= monthStart && e.RegisteredAt < nextMonth)
                .Select(e => e.RegisteredAt)
                .ToListAsync();
            var policyDates = await _db.Policies
                .Where(p => p.CreatedAt >= monthStart && p.CreatedAt < nextMonth)
                .Select(p => p.CreatedAt)
                .ToListAsync();
            var requestDates = await _db.PolicyRequestDetails
                .Where(r => r.RequestedAt >= monthStart && r.RequestedAt < nextMonth)
                .Select(r => r.RequestedAt)
                .ToListAsync();
            var billDates = await _db.PolicyBills
                .Where(b => b.CreatedAt >= monthStart && b.CreatedAt < nextMonth)
                .Select(b => b.CreatedAt)
                .ToListAsync();
            var claimsProcessedThisMonth = await _db.PolicyBills
                .CountAsync(b => (b.PaidAt.HasValue && b.PaidAt.Value >= monthStart && b.PaidAt.Value < nextMonth)
                    || (b.ClosedAt.HasValue && b.ClosedAt.Value >= monthStart && b.ClosedAt.Value < nextMonth));
            var overviewTrend = BuildMonthlyOverviewTrend(
                monthStart,
                now.Date,
                employeeDates.Concat(policyDates).Concat(requestDates).Concat(billDates));

            var vm = new AdminDashboardViewModel
            {
                TotalEmployees = await _db.EmpRegisters.CountAsync(e => e.IsActive),
                TotalCompanies = await _db.CompanyDetails.CountAsync(c => c.IsActive),
                TotalPolicies  = await _db.Policies.CountAsync(p => p.IsActive),
                PendingRequests = await _db.PolicyRequestDetails.CountAsync(r => r.Status == "Pending"),
                PendingBills = await _db.PolicyBills.CountAsync(b => b.Status == "Created" || b.Status == "ManagerApproved"),
                NewEmployeesThisMonth = employeeDates.Count,
                NewPoliciesThisMonth = policyDates.Count,
                ClaimsProcessedThisMonth = claimsProcessedThisMonth,
                BillsGeneratedThisMonth = billDates.Count,
                OverviewTrend = overviewTrend,
                RecentRequests = await _db.PolicyRequestDetails
                    .Include(r => r.Employee)
                    .Include(r => r.Policy)
                    .Include(r => r.Bill)
                    .OrderByDescending(r => r.RequestedAt)
                    .Take(5)
                    .ToListAsync(),
                RecentEmployees = await _db.EmpRegisters
                    .Where(e => e.IsActive)
                    .OrderByDescending(e => e.RegisteredAt)
                    .Take(5)
                    .ToListAsync(),
                RecentCompanies = await _db.CompanyDetails
                    .Where(c => c.IsActive)
                    .OrderByDescending(c => c.RegisteredAt)
                    .Take(5)
                    .ToListAsync(),
                RecentPolicies = await _db.Policies
                    .Where(p => p.IsActive)
                    .Include(p => p.Company)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };
            return View(vm);
        }

        private static List<DashboardTrendPoint> BuildMonthlyOverviewTrend(
            DateTime monthStart,
            DateTime today,
            IEnumerable<DateTime> eventDates)
        {
            var lastDay = today < monthStart ? monthStart : today;
            var dailyCounts = eventDates
                .GroupBy(date => date.Date)
                .ToDictionary(group => group.Key, group => group.Count());

            var trend = new List<DashboardTrendPoint>();
            for (var day = monthStart.Date; day <= lastDay.Date; day = day.AddDays(1))
            {
                dailyCounts.TryGetValue(day, out var value);
                trend.Add(new DashboardTrendPoint
                {
                    Date = day,
                    Value = value
                });
            }

            return trend;
        }

        // ══════════════════════════════════════════════════════════════
        //  INSURANCE COMPANIES  (Add Resource)
        // ══════════════════════════════════════════════════════════════

        public async Task<IActionResult> Companies()
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            var companies = await _db.CompanyDetails.OrderBy(c => c.CompanyName).ToListAsync();
            return View(companies);
        }

        [HttpGet]
        public IActionResult AddCompany()
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            return View(new CompanyDetails());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCompany(CompanyDetails model)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            if (!ModelState.IsValid) return View(model);

            _db.CompanyDetails.Add(model);
            await _db.SaveChangesAsync();
            await _notifications.NotifyAdminsAsync(
                "Insurance company added",
                $"""
                <p>A new insurance company was added.</p>
                <p><strong>Company:</strong> {model.CompanyName}</p>
                <p><strong>Email:</strong> {model.Email}</p>
                """,
                "CompanyAdded",
                model.CompanyId);
            TempData["Success"] = "Insurance company added successfully.";
            return RedirectToAction("Companies");
        }

        [HttpGet]
        public async Task<IActionResult> EditCompany(int id)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            var company = await _db.CompanyDetails.FindAsync(id);
            if (company == null) return NotFound();
            return View(company);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCompany(CompanyDetails model)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            if (!ModelState.IsValid) return View(model);

            _db.CompanyDetails.Update(model);
            await _db.SaveChangesAsync();
            await _notifications.NotifyAdminsAsync(
                "Insurance company updated",
                $"""
                <p>An insurance company was updated.</p>
                <p><strong>Company:</strong> {model.CompanyName}</p>
                <p><strong>Email:</strong> {model.Email}</p>
                """,
                "CompanyUpdated",
                model.CompanyId);
            TempData["Success"] = "Company updated successfully.";
            return RedirectToAction("Companies");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCompany(int id)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            var company = await _db.CompanyDetails.FindAsync(id);
            if (company != null)
            {
                company.IsActive = false;
                await _db.SaveChangesAsync();
                await _notifications.NotifyAdminsAsync(
                    "Insurance company deactivated",
                    $"<p><strong>{company.CompanyName}</strong> was deactivated.</p>",
                    "CompanyDeactivated",
                    company.CompanyId);
            }
            TempData["Success"] = "Company deactivated.";
            return RedirectToAction("Companies");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateCompany(int id)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            var company = await _db.CompanyDetails.FindAsync(id);
            if (company != null)
            {
                company.IsActive = true;
                await _db.SaveChangesAsync();
                await _notifications.NotifyAdminsAsync(
                    "Insurance company activated",
                    $"<p><strong>{company.CompanyName}</strong> was activated.</p>",
                    "CompanyActivated",
                    company.CompanyId);
            }
            TempData["Success"] = "Company activated.";
            return RedirectToAction("Companies");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PermanentlyDeleteCompany(int id)
        {
            var guard = AdminGuard(); if (guard != null) return guard;

            var company = await _db.CompanyDetails.FindAsync(id);
            if (company == null)
            {
                TempData["Error"] = "Company not found.";
                return RedirectToAction("Companies");
            }

            var hasPolicies = await _db.Policies.AnyAsync(p => p.CompanyId == id);
            if (hasPolicies)
            {
                TempData["Error"] = "This company cannot be deleted from the database because policies are linked to it.";
                return RedirectToAction("Companies");
            }

            _db.CompanyDetails.Remove(company);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Company deleted from database.";
            return RedirectToAction("Companies");
        }

        // ══════════════════════════════════════════════════════════════
        //  POLICIES
        // ══════════════════════════════════════════════════════════════

        public async Task<IActionResult> Policies()
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            var policies = await _db.Policies
                .Include(p => p.Company)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(policies);
        }

        [HttpGet]
        public async Task<IActionResult> AddPolicy()
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            ViewBag.Companies = await _db.CompanyDetails.Where(c => c.IsActive).ToListAsync();
            return View(new Policy());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPolicy(Policy model)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            if (!ModelState.IsValid)
            {
                ViewBag.Companies = await _db.CompanyDetails.Where(c => c.IsActive).ToListAsync();
                return View(model);
            }
            _db.Policies.Add(model);
            await _db.SaveChangesAsync();
            var addedPolicy = await _db.Policies
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.PolicyId == model.PolicyId);
            if (addedPolicy != null)
            {
                var body = $"""
                    <p>A new policy is available.</p>
                    <p><strong>Policy:</strong> {addedPolicy.PolicyName}</p>
                    <p><strong>Company:</strong> {addedPolicy.Company?.CompanyName ?? "N/A"}</p>
                    <p><strong>Premium:</strong> PKR {addedPolicy.PremiumAmount:N0}</p>
                    """;
                await _notifications.NotifyAdminsAsync("Policy added", body, "PolicyAdded", addedPolicy.PolicyId);
                await _notifications.NotifyAllStaffAsync("New insurance policy available", body, "PolicyAdded", addedPolicy.PolicyId);
            }
            TempData["Success"] = "Policy added successfully.";
            return RedirectToAction("Policies");
        }

        [HttpGet]
        public async Task<IActionResult> EditPolicy(int id)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            var policy = await _db.Policies.FindAsync(id);
            if (policy == null) return NotFound();
            ViewBag.Companies = await _db.CompanyDetails.Where(c => c.IsActive).ToListAsync();
            return View(policy);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPolicy(Policy model)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            if (!ModelState.IsValid)
            {
                ViewBag.Companies = await _db.CompanyDetails.Where(c => c.IsActive).ToListAsync();
                return View(model);
            }
            _db.Policies.Update(model);
            await _db.SaveChangesAsync();
            var updatedPolicy = await _db.Policies
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.PolicyId == model.PolicyId);
            if (updatedPolicy != null)
            {
                var body = $"""
                    <p>An insurance policy was updated.</p>
                    <p><strong>Policy:</strong> {updatedPolicy.PolicyName}</p>
                    <p><strong>Company:</strong> {updatedPolicy.Company?.CompanyName ?? "N/A"}</p>
                    <p><strong>Premium:</strong> PKR {updatedPolicy.PremiumAmount:N0}</p>
                    """;
                await _notifications.NotifyAdminsAsync("Policy updated", body, "PolicyUpdated", updatedPolicy.PolicyId);
                await _notifications.NotifyAllStaffAsync("Insurance policy updated", body, "PolicyUpdated", updatedPolicy.PolicyId);
            }
            TempData["Success"] = "Policy updated.";
            return RedirectToAction("Policies");
        }

        // ══════════════════════════════════════════════════════════════
        //  EMPLOYEES  (Employee Support)
        // ══════════════════════════════════════════════════════════════

        public async Task<IActionResult> Employees()
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            var employees = await _db.EmpRegisters
                .OrderBy(e => e.FirstName)
                .ToListAsync();
            return View(employees);
        }

        [HttpGet]
        public IActionResult RegisterEmployee()
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            ViewBag.Roles = UserRoles.StaffRoles;
            return View(new EmpRegister());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterEmployee(EmpRegister model, string Password)
        {
            var guard = AdminGuard(); if (guard != null) return guard;

            // Remove PasswordHash from validation as we set it manually
            ModelState.Remove("PasswordHash");
            if (!UserRoles.StaffRoles.Contains(model.Role))
                ModelState.AddModelError("Role", "Select a valid role.");

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = UserRoles.StaffRoles;
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
            {
                ModelState.AddModelError("", "Password must be at least 6 characters.");
                ViewBag.Roles = UserRoles.StaffRoles;
                return View(model);
            }

            // Check duplicate username / email
            if (await _db.EmpRegisters.AnyAsync(e => e.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Username already exists.");
                ViewBag.Roles = UserRoles.StaffRoles;
                return View(model);
            }
            if (await _db.EmpRegisters.AnyAsync(e => e.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already registered.");
                ViewBag.Roles = UserRoles.StaffRoles;
                return View(model);
            }

            model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password);
            _db.EmpRegisters.Add(model);
            await _db.SaveChangesAsync();
            var employeeName = FullName(model);
            var roleName = UserRoles.DisplayName(model.Role);
            await _notifications.NotifyAdminsAsync(
                "Staff account registered",
                $"""
                <p>A new staff account was registered.</p>
                <p><strong>Name:</strong> {employeeName}</p>
                <p><strong>Role:</strong> {roleName}</p>
                <p><strong>Email:</strong> {model.Email}</p>
                """,
                "EmployeeRegistered",
                model.EmpId);
            await _notifications.NotifyAsync(
                model.Email,
                employeeName,
                model.Role,
                "Your Health Insurance account was created",
                $"""
                <p>Your account has been created.</p>
                <p><strong>Username:</strong> {model.Username}</p>
                <p><strong>Role:</strong> {roleName}</p>
                <p>You can now log in to your dashboard.</p>
                """,
                "EmployeeRegistered",
                model.EmpId);
            TempData["Success"] = $"Employee {model.FirstName} {model.LastName} registered successfully.";
            return RedirectToAction("Employees");
        }

        [HttpGet]
        public async Task<IActionResult> EditEmployee(int id)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            var emp = await _db.EmpRegisters.FindAsync(id);
            if (emp == null) return NotFound();
            ViewBag.Roles = UserRoles.StaffRoles;
            return View(emp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(EmpRegister model)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            ModelState.Remove("PasswordHash");
            ModelState.Remove("Username");
            if (!UserRoles.StaffRoles.Contains(model.Role))
                ModelState.AddModelError("Role", "Select a valid role.");
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = UserRoles.StaffRoles;
                return View(model);
            }

            var existing = await _db.EmpRegisters.FindAsync(model.EmpId);
            if (existing == null) return NotFound();

            existing.FirstName   = model.FirstName;
            existing.LastName    = model.LastName;
            existing.Email       = model.Email;
            existing.Phone       = model.Phone;
            existing.Department  = model.Department;
            existing.Designation = model.Designation;
            existing.Role        = model.Role;
            existing.Address     = model.Address;
            existing.Gender      = model.Gender;
            existing.DateOfBirth = model.DateOfBirth;

            await _db.SaveChangesAsync();
            await _notifications.NotifyAdminsAsync(
                "Staff profile updated",
                $"""
                <p>A staff profile was updated.</p>
                <p><strong>Name:</strong> {FullName(existing)}</p>
                <p><strong>Role:</strong> {UserRoles.DisplayName(existing.Role)}</p>
                <p><strong>Email:</strong> {existing.Email}</p>
                """,
                "EmployeeUpdated",
                existing.EmpId);
            await _notifications.NotifyAsync(
                existing.Email,
                FullName(existing),
                existing.Role,
                "Your profile was updated",
                "<p>Your account details were updated by an administrator.</p>",
                "EmployeeUpdated",
                existing.EmpId);
            TempData["Success"] = "Employee updated successfully.";
            return RedirectToAction("Employees");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            var deleted = await DeleteEmployeeAndRelatedInfoAsync(id);
            TempData[deleted ? "Success" : "Error"] = deleted
                ? "Employee and all linked information deleted from the database."
                : "Employee not found.";
            return RedirectToAction("Employees");
        }

        private async Task<bool> DeleteEmployeeAndRelatedInfoAsync(int id)
        {
            var employee = await _db.EmpRegisters.FindAsync(id);
            if (employee == null) return false;

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var requestIds = await _db.PolicyRequestDetails
                .Where(r => r.EmpId == id)
                .Select(r => r.RequestId)
                .ToListAsync();

            if (requestIds.Any())
            {
                var requestBills = await _db.PolicyBills
                    .Where(b => requestIds.Contains(b.RequestId))
                    .ToListAsync();
                _db.PolicyBills.RemoveRange(requestBills);

                var requestApprovals = await _db.PolicyApprovalDetails
                    .Where(a => requestIds.Contains(a.RequestId))
                    .ToListAsync();
                _db.PolicyApprovalDetails.RemoveRange(requestApprovals);

                var requests = await _db.PolicyRequestDetails
                    .Where(r => r.EmpId == id)
                    .ToListAsync();
                _db.PolicyRequestDetails.RemoveRange(requests);
            }

            var assignments = await _db.PolicyOnEmployees
                .Where(pe => pe.EmpId == id)
                .ToListAsync();
            _db.PolicyOnEmployees.RemoveRange(assignments);

            var managerApprovals = await _db.PolicyApprovalDetails
                .Where(pa => pa.ManagerId == id)
                .ToListAsync();
            _db.PolicyApprovalDetails.RemoveRange(managerApprovals);

            var staffBills = await _db.PolicyBills
                .Where(b => !requestIds.Contains(b.RequestId)
                    && (b.ManagerId == id || b.FinanceManagerId == id))
                .ToListAsync();
            foreach (var bill in staffBills)
            {
                if (bill.ManagerId == id) bill.ManagerId = null;
                if (bill.FinanceManagerId == id) bill.FinanceManagerId = null;
            }

            _db.EmpRegisters.Remove(employee);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateEmployee(int id)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            var employee = await _db.EmpRegisters.FindAsync(id);
            if (employee != null)
            {
                employee.IsActive = false;
                await _db.SaveChangesAsync();
                await _notifications.NotifyAdminsAsync(
                    "Staff account deactivated",
                    $"<p><strong>{FullName(employee)}</strong> was deactivated.</p>",
                    "EmployeeDeactivated",
                    employee.EmpId);
                await _notifications.NotifyAsync(
                    employee.Email,
                    FullName(employee),
                    employee.Role,
                    "Your account was deactivated",
                    "<p>Your Health Insurance Management account has been deactivated by an administrator.</p>",
                    "EmployeeDeactivated",
                    employee.EmpId);
            }
            TempData["Success"] = "Employee deactivated.";
            return RedirectToAction("Employees");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateEmployee(int id)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            var employee = await _db.EmpRegisters.FindAsync(id);
            if (employee != null)
            {
                employee.IsActive = true;
                await _db.SaveChangesAsync();
                await _notifications.NotifyAdminsAsync(
                    "Staff account activated",
                    $"<p><strong>{FullName(employee)}</strong> was activated.</p>",
                    "EmployeeActivated",
                    employee.EmpId);
                await _notifications.NotifyAsync(
                    employee.Email,
                    FullName(employee),
                    employee.Role,
                    "Your account was activated",
                    "<p>Your Health Insurance Management account has been activated.</p>",
                    "EmployeeActivated",
                    employee.EmpId);
            }
            TempData["Success"] = "Employee activated.";
            return RedirectToAction("Employees");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PermanentlyDeleteEmployee(int id)
        {
            var guard = AdminGuard(); if (guard != null) return guard;

            var deleted = await DeleteEmployeeAndRelatedInfoAsync(id);
            TempData[deleted ? "Success" : "Error"] = deleted
                ? "Employee and all linked information deleted from the database."
                : "Employee not found.";
            return RedirectToAction("Employees");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePolicy(int id)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            var policy = await _db.Policies.FindAsync(id);
            if (policy != null)
            {
                policy.IsActive = false;
                await _db.SaveChangesAsync();
                await _notifications.NotifyAdminsAsync(
                    "Policy deactivated",
                    $"<p><strong>{policy.PolicyName}</strong> was deactivated.</p>",
                    "PolicyDeactivated",
                    policy.PolicyId);
            }
            TempData["Success"] = "Policy deactivated.";
            return RedirectToAction("Policies");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivatePolicy(int id)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            var policy = await _db.Policies.FindAsync(id);
            if (policy != null)
            {
                policy.IsActive = true;
                await _db.SaveChangesAsync();
                await _notifications.NotifyAdminsAsync(
                    "Policy activated",
                    $"<p><strong>{policy.PolicyName}</strong> was activated.</p>",
                    "PolicyActivated",
                    policy.PolicyId);
                await _notifications.NotifyAllStaffAsync(
                    "Insurance policy activated",
                    $"<p><strong>{policy.PolicyName}</strong> is now active and available in the system.</p>",
                    "PolicyActivated",
                    policy.PolicyId);
            }
            TempData["Success"] = "Policy activated.";
            return RedirectToAction("Policies");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PermanentlyDeletePolicy(int id)
        {
            var guard = AdminGuard(); if (guard != null) return guard;

            var policy = await _db.Policies.FindAsync(id);
            if (policy == null)
            {
                TempData["Error"] = "Policy not found.";
                return RedirectToAction("Policies");
            }

            var isAssigned = await _db.PolicyOnEmployees.AnyAsync(pe => pe.PolicyId == id);
            var hasRequests = await _db.PolicyRequestDetails.AnyAsync(pr => pr.PolicyId == id);

            if (isAssigned || hasRequests)
            {
                TempData["Error"] = "This policy cannot be deleted from the database because linked assignments or requests still exist.";
                return RedirectToAction("Policies");
            }

            _db.Policies.Remove(policy);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Policy deleted from database.";
            return RedirectToAction("Policies");
        }

        [HttpGet]
        public async Task<IActionResult> Search(string? q)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            q = q?.Trim();
            var vm = new AdminSearchViewModel { Query = q };

            if (!string.IsNullOrWhiteSpace(q))
            {
                vm.Employees = await _db.EmpRegisters
                    .Where(e => e.FirstName.Contains(q) || e.LastName.Contains(q) || e.Username.Contains(q) || e.Email.Contains(q) || e.Department.Contains(q) || e.Role.Contains(q))
                    .OrderBy(e => e.FirstName)
                    .Take(50)
                    .ToListAsync();

                vm.Companies = await _db.CompanyDetails
                    .Where(c => c.CompanyName.Contains(q) || c.Email.Contains(q) || c.ContactNumber.Contains(q))
                    .OrderBy(c => c.CompanyName)
                    .Take(50)
                    .ToListAsync();

                vm.Policies = await _db.Policies
                    .Include(p => p.Company)
                    .Where(p => p.PolicyName.Contains(q) || p.PolicyType.Contains(q) || (p.Company != null && p.Company.CompanyName.Contains(q)))
                    .OrderBy(p => p.PolicyName)
                    .Take(50)
                    .ToListAsync();

                vm.Requests = await _db.PolicyRequestDetails
                    .Include(r => r.Employee)
                    .Include(r => r.Policy)
                    .Where(r => r.Status.Contains(q) || (r.Employee != null && (r.Employee.FirstName.Contains(q) || r.Employee.LastName.Contains(q))) || (r.Policy != null && r.Policy.PolicyName.Contains(q)))
                    .OrderByDescending(r => r.RequestedAt)
                    .Take(50)
                    .ToListAsync();

                vm.Bills = await _db.PolicyBills
                    .Include(b => b.PolicyRequest).ThenInclude(r => r!.Employee)
                    .Include(b => b.PolicyRequest).ThenInclude(r => r!.Policy)
                    .Where(b => b.Status.Contains(q) || (b.PolicyRequest != null && b.PolicyRequest.Employee != null && (b.PolicyRequest.Employee.FirstName.Contains(q) || b.PolicyRequest.Employee.LastName.Contains(q))))
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(50)
                    .ToListAsync();
            }

            return View(vm);
        }

        // Assign a policy to an employee
        [HttpGet]
        public async Task<IActionResult> AssignPolicy(int empId)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            ViewBag.Employee = await _db.EmpRegisters.FindAsync(empId);
            ViewBag.Policies = await _db.Policies.Include(p => p.Company).Where(p => p.IsActive).ToListAsync();
            return View(new PolicyOnEmployee { EmpId = empId, StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(12) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPolicy(PolicyOnEmployee model)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            if (!ModelState.IsValid)
            {
                ViewBag.Employee = await _db.EmpRegisters.FindAsync(model.EmpId);
                ViewBag.Policies = await _db.Policies.Include(p => p.Company).Where(p => p.IsActive).ToListAsync();
                return View(model);
            }
            _db.PolicyOnEmployees.Add(model);
            await _db.SaveChangesAsync();
            var assignment = await _db.PolicyOnEmployees
                .Include(pe => pe.Employee)
                .Include(pe => pe.Policy).ThenInclude(p => p!.Company)
                .FirstOrDefaultAsync(pe => pe.Id == model.Id);
            if (assignment?.Employee != null && assignment.Policy != null)
            {
                var employee = assignment.Employee;
                var employeeName = FullName(employee);
                var body = $"""
                    <p>A policy was assigned to an employee.</p>
                    <p><strong>Employee:</strong> {employeeName}</p>
                    <p><strong>Policy:</strong> {assignment.Policy.PolicyName}</p>
                    <p><strong>Company:</strong> {assignment.Policy.Company?.CompanyName ?? "N/A"}</p>
                    <p><strong>Coverage Dates:</strong> {assignment.StartDate:yyyy-MM-dd} to {assignment.EndDate:yyyy-MM-dd}</p>
                    """;

                await _notifications.NotifyAdminsAsync("Policy assigned to employee", body, "PolicyAssigned", assignment.Id);
                await _notifications.NotifyAsync(
                    employee.Email,
                    employeeName,
                    employee.Role,
                    "A policy was assigned to you",
                    $"""
                    <p><strong>{assignment.Policy.PolicyName}</strong> has been assigned to you.</p>
                    <p><strong>Company:</strong> {assignment.Policy.Company?.CompanyName ?? "N/A"}</p>
                    <p><strong>Coverage Dates:</strong> {assignment.StartDate:yyyy-MM-dd} to {assignment.EndDate:yyyy-MM-dd}</p>
                    """,
                    "PolicyAssigned",
                    assignment.Id);
            }
            TempData["Success"] = "Policy assigned to employee.";
            return RedirectToAction("Employees");
        }

        // ══════════════════════════════════════════════════════════════
        //  POLICY REQUESTS  (Approval / Rejection)
        // ══════════════════════════════════════════════════════════════

        public async Task<IActionResult> PolicyRequests()
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            var requests = await _db.PolicyRequestDetails
                .Include(r => r.Employee)
                .Include(r => r.Policy).ThenInclude(p => p!.Company)
                .Include(r => r.Approval)
                .Include(r => r.Bill)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRequest(int requestId, string decision, string? remarks)
        {
            var guard = AdminGuard(); if (guard != null) return guard;

            var request = await _db.PolicyRequestDetails
                .Include(r => r.Employee)
                .Include(r => r.Policy)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);
            if (request == null) return NotFound();

            request.Status = decision; // "Approved" or "Rejected"

            var bill = await _db.PolicyBills.FirstOrDefaultAsync(b => b.RequestId == requestId);
            if (bill != null)
            {
                bill.Status = decision == "Approved" ? "ManagerApproved" : "Rejected";
                bill.Remarks = remarks;
            }

            if (decision == "Approved")
            {
                var policy = await _db.Policies.FindAsync(request.PolicyId);
                if (policy != null)
                {
                    _db.PolicyOnEmployees.Add(new PolicyOnEmployee
                    {
                        EmpId     = request.EmpId,
                        PolicyId  = request.PolicyId,
                        StartDate = DateTime.Today,
                        EndDate   = DateTime.Today.AddMonths(policy.DurationMonths),
                        Status    = "Active"
                    });
                }
            }

            await _db.SaveChangesAsync();
            if (request.Employee != null && request.Policy != null)
            {
                var employee = request.Employee;
                var employeeName = FullName(employee);
                var body = $"""
                    <p>An admin processed a policy request.</p>
                    <p><strong>Decision:</strong> {decision}</p>
                    <p><strong>Employee:</strong> {employeeName}</p>
                    <p><strong>Policy:</strong> {request.Policy.PolicyName}</p>
                    <p><strong>Bill Amount:</strong> PKR {request.BillAmount:N0}</p>
                    """;

                await _notifications.NotifyAdminsAsync($"Policy request {decision}", body, "AdminRequestDecision", request.RequestId);
                await _notifications.NotifyAsync(
                    employee.Email,
                    employeeName,
                    employee.Role,
                    $"Your policy request was {decision}",
                    $"""
                    <p>Your request for <strong>{request.Policy.PolicyName}</strong> was <strong>{decision}</strong>.</p>
                    <p><strong>Bill Amount:</strong> PKR {request.BillAmount:N0}</p>
                    """,
                    "AdminRequestDecision",
                    request.RequestId);
            }
            TempData["Success"] = $"Request {decision} successfully.";
            return RedirectToAction("PolicyRequests");
        }

        private static string FullName(EmpRegister employee)
        {
            return $"{employee.FirstName} {employee.LastName}".Trim();
        }
    }
}
