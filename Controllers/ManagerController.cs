using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthInsuranceManagement.Data;
using HealthInsuranceManagement.Models;
using HealthInsuranceManagement.Models.ViewModels;

namespace HealthInsuranceManagement.Controllers
{
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ManagerController(ApplicationDbContext db)
        {
            _db = db;
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

            var vm = new ManagerDashboardViewModel
            {
                Manager = await _db.EmpRegisters.FindAsync(CurrentManagerId),
                ReceivedBills = await _db.PolicyBills
                    .Include(b => b.PolicyRequest).ThenInclude(r => r!.Employee)
                    .Include(b => b.PolicyRequest).ThenInclude(r => r!.Policy)
                    .ThenInclude(p => p!.Company)
                    .Where(b => b.Status == "Created" || b.Status == "ManagerApproved" || b.ManagerId == CurrentManagerId)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync()
            };

            return View(vm);
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
    }
}
