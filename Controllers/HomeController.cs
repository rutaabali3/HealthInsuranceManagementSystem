using Microsoft.AspNetCore.Mvc;
using HealthInsuranceManagement.Data;
using HealthInsuranceManagement.Models;
using HealthInsuranceManagement.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HealthInsuranceManagement.Controllers
{
    /// <summary>
    /// Handles public-facing pages: Home, About, Contact, Feedback.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View(new ContactFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var query = new ContactQuery
            {
                Name = model.ContactName.Trim(),
                Email = model.ContactEmail.Trim(),
                Message = model.ContactMessage.Trim(),
                Status = "New",
                CreatedAt = DateTime.UtcNow
            };

            _db.ContactQueries.Add(query);
            _db.ContactMessages.Add(new ContactMessage
            {
                ContactQuery = query,
                SenderType = "Visitor",
                Message = query.Message,
                SentAt = query.CreatedAt
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = "Your message has been saved. Our team will reply to your email soon.";
            return RedirectToAction(nameof(Contact));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        public async Task<IActionResult> Faq()
        {
            var faqs = DefaultFaqItems();

            try
            {
                var managedFaqs = await _db.FaqItems
                    .Where(f => f.IsActive)
                    .OrderBy(f => f.Category)
                    .ThenBy(f => f.DisplayOrder)
                    .ThenBy(f => f.Question)
                    .ToListAsync();

                if (managedFaqs.Any())
                {
                    faqs = managedFaqs;
                }
            }
            catch
            {
                // The fallback keeps the public FAQ usable before the FAQ migration is applied.
            }

            return View(new FaqPageViewModel { Items = faqs });
        }

        public IActionResult Sitemap()
        {
            return View();
        }

        public IActionResult Feedback()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult NotFound404()
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }

        private static List<FaqItem> DefaultFaqItems()
        {
            return new List<FaqItem>
            {
                new() { Category = "Accounts and Access", DisplayOrder = 1, Question = "Who can use HealthInsure?", Answer = "HealthInsure is for authorized users in the insurance workflow. Admins manage employees, companies, policies, assignments, requests, and reports. Employees search and request policies. Managers and finance users handle decision and payment stages. Support users manage contact queries." },
                new() { Category = "Accounts and Access", DisplayOrder = 2, Question = "Why do different users see different menus?", Answer = "Navigation is role-based. A user only sees the dashboard and actions intended for their assigned role, which keeps sensitive employee, policy, request, and billing areas from being exposed to unrelated users." },
                new() { Category = "Employee Policy Requests", DisplayOrder = 1, Question = "How does an employee request insurance?", Answer = "The employee signs in, opens Search Policies, reviews available policies, opens policy details, and submits a request for the selected policy. The request then moves into the approval workflow." },
                new() { Category = "Approvals and Workflow", DisplayOrder = 1, Question = "What happens after a policy request is submitted?", Answer = "The request is saved with the employee and policy details. Authorized staff can review the request status and continue the workflow through manager and finance stages when applicable." },
                new() { Category = "Finance and Billing", DisplayOrder = 1, Question = "Who handles finance actions?", Answer = "Finance users handle approved billing records assigned to their workflow. They can review request details, credit payments, and close records when the required payment step is complete." },
                new() { Category = "Reports and Admin Data", DisplayOrder = 1, Question = "What can administrators manage?", Answer = "Administrators can manage employee accounts, insurance companies, policies, policy assignments, policy requests, reports, and system-level records needed for the organization." },
                new() { Category = "Support, Privacy, and Troubleshooting", DisplayOrder = 1, Question = "How do visitors contact support?", Answer = "Visitors can use the contact page to send a message. Support users can then review the inbox, open the query, and reply using the stored conversation." }
            };
        }
    }
}
