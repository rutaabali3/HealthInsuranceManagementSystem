using Microsoft.AspNetCore.Mvc;
using HealthInsuranceManagement.Data;
using HealthInsuranceManagement.Models;
using HealthInsuranceManagement.Models.ViewModels;

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
    }
}
