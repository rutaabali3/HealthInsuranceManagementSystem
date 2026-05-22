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
