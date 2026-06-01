using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthInsuranceManagement.Data;
using HealthInsuranceManagement.Models;
using HealthInsuranceManagement.Models.ViewModels;

namespace HealthInsuranceManagement.Controllers
{
    /// <summary>
    /// Generates date-based reports for the admin: employee list and policy request history.
    /// </summary>
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ReportController(ApplicationDbContext db)
        {
            _db = db;
        }

        private IActionResult? AdminGuard()
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
                return RedirectToAction("Login", "Auth");
            return null;
        }

        // GET: /Report/Index
        [HttpGet]
        public IActionResult Index()
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            return View(new ReportViewModel());
        }

        // POST: /Report/Index
        [HttpPost]
        public async Task<IActionResult> Index(ReportViewModel model)
        {
            var guard = AdminGuard(); if (guard != null) return guard;
            if (!ModelState.IsValid)
                return View(model);

            // Employee report: all active employees registered in date range
            var empQuery = _db.EmpRegisters.Where(e => e.IsActive);
            if (model.FromDate.HasValue)
                empQuery = empQuery.Where(e => e.RegisteredAt >= model.FromDate.Value);
            if (model.ToDate.HasValue)
                empQuery = empQuery.Where(e => e.RegisteredAt <= model.ToDate.Value.AddDays(1));

            model.EmployeeReport = await empQuery.OrderBy(e => e.FirstName).ToListAsync();

            // Request report: policy requests in date range
            var reqQuery = _db.PolicyRequestDetails
                .Include(r => r.Employee)
                .Include(r => r.Policy)
                .AsQueryable();
            if (model.FromDate.HasValue)
                reqQuery = reqQuery.Where(r => r.RequestedAt >= model.FromDate.Value);
            if (model.ToDate.HasValue)
                reqQuery = reqQuery.Where(r => r.RequestedAt <= model.ToDate.Value.AddDays(1));

            model.RequestReport = await reqQuery.OrderByDescending(r => r.RequestedAt).ToListAsync();

            var billQuery = _db.PolicyBills
                .Include(b => b.PolicyRequest).ThenInclude(r => r!.Employee)
                .Include(b => b.PolicyRequest).ThenInclude(r => r!.Policy)
                .Include(b => b.Manager)
                .Include(b => b.FinanceManager)
                .AsQueryable();
            if (model.FromDate.HasValue)
                billQuery = billQuery.Where(b => b.CreatedAt >= model.FromDate.Value);
            if (model.ToDate.HasValue)
                billQuery = billQuery.Where(b => b.CreatedAt <= model.ToDate.Value.AddDays(1));

            model.BillingReport = await billQuery.OrderByDescending(b => b.CreatedAt).ToListAsync();

            var claimQuery = _db.InsuranceClaims
                .Include(c => c.Employee)
                .Include(c => c.AssignedPolicy).ThenInclude(pe => pe!.Policy)
                .Include(c => c.Manager)
                .Include(c => c.FinanceManager)
                .AsQueryable();
            if (model.FromDate.HasValue)
                claimQuery = claimQuery.Where(c => c.SubmittedAt >= model.FromDate.Value);
            if (model.ToDate.HasValue)
                claimQuery = claimQuery.Where(c => c.SubmittedAt <= model.ToDate.Value.AddDays(1));

            model.ClaimReport = await claimQuery.OrderByDescending(c => c.SubmittedAt).ToListAsync();

            return View(model);
        }
    }
}
