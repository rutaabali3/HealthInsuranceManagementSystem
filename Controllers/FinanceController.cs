using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthInsuranceManagement.Data;
using HealthInsuranceManagement.Models;
using HealthInsuranceManagement.Models.ViewModels;

namespace HealthInsuranceManagement.Controllers
{
    public class FinanceController : Controller
    {
        private readonly ApplicationDbContext _db;

        public FinanceController(ApplicationDbContext db)
        {
            _db = db;
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

            var vm = new FinanceDashboardViewModel
            {
                FinanceManager = await _db.EmpRegisters.FindAsync(CurrentFinanceManagerId),
                ReceivedBills = await _db.PolicyBills
                    .Include(b => b.PolicyRequest).ThenInclude(r => r!.Employee)
                    .Include(b => b.PolicyRequest).ThenInclude(r => r!.Policy)
                    .ThenInclude(p => p!.Company)
                    .Include(b => b.Manager)
                    .Where(b => b.Status == "ForwardedToFinance" || b.FinanceManagerId == CurrentFinanceManagerId)
                    .OrderByDescending(b => b.ForwardedAt ?? b.CreatedAt)
                    .ToListAsync()
            };

            return View(vm);
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

            TempData["Success"] = "Request closed after payment.";
            return RedirectToAction("Dashboard");
        }
    }
}
