using HealthInsuranceManagement.Data;
using HealthInsuranceManagement.Models;
using HealthInsuranceManagement.Models.ViewModels;
using HealthInsuranceManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthInsuranceManagement.Controllers
{
    public class SupportController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;

        public SupportController(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

        private IActionResult? SupportGuard()
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Support)
                return RedirectToAction("Login", "Auth");
            return null;
        }

        private int CurrentSupportId => HttpContext.Session.GetInt32("UserId") ?? 0;

        public async Task<IActionResult> Dashboard()
        {
            var guard = SupportGuard(); if (guard != null) return guard;
            var activeFaqs = 0;

            try
            {
                activeFaqs = await _db.FaqItems.CountAsync(f => f.IsActive);
            }
            catch
            {
                // Keeps the dashboard available before the FAQ migration is applied.
            }

            var vm = new SupportDashboardViewModel
            {
                SupportUser = await _db.EmpRegisters.FindAsync(CurrentSupportId),
                NewQueries = await _db.ContactQueries.CountAsync(q => q.Status == "New"),
                OpenQueries = await _db.ContactQueries.CountAsync(q => q.Status != "Closed"),
                RepliedQueries = await _db.ContactQueries.CountAsync(q => q.Status == "Replied"),
                ActiveFaqs = activeFaqs,
                RecentQueries = await _db.ContactQueries
                    .OrderByDescending(q => q.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };

            return View(vm);
        }

        public async Task<IActionResult> Faqs()
        {
            var guard = SupportGuard(); if (guard != null) return guard;

            var faqs = await _db.FaqItems
                .OrderBy(f => f.Category)
                .ThenBy(f => f.DisplayOrder)
                .ThenBy(f => f.Question)
                .ToListAsync();

            return View(new SupportFaqListViewModel
            {
                Faqs = faqs,
                ActiveCount = faqs.Count(f => f.IsActive),
                HiddenCount = faqs.Count(f => !f.IsActive)
            });
        }

        [HttpGet]
        public IActionResult AddFaq()
        {
            var guard = SupportGuard(); if (guard != null) return guard;
            return View("FaqForm", new FaqItem { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFaq(FaqItem model)
        {
            var guard = SupportGuard(); if (guard != null) return guard;
            PrepareFaqModel(model);

            if (!ModelState.IsValid)
            {
                return View("FaqForm", model);
            }

            model.CreatedAt = DateTime.UtcNow;
            model.CreatedBySupportId = CurrentSupportId == 0 ? null : CurrentSupportId;
            _db.FaqItems.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = "FAQ has been added.";
            return RedirectToAction(nameof(Faqs));
        }

        [HttpGet]
        public async Task<IActionResult> EditFaq(int id)
        {
            var guard = SupportGuard(); if (guard != null) return guard;
            var faq = await _db.FaqItems.FindAsync(id);
            if (faq == null) return NotFound();
            return View("FaqForm", faq);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFaq(int id, FaqItem model)
        {
            var guard = SupportGuard(); if (guard != null) return guard;
            if (id != model.Id) return BadRequest();

            PrepareFaqModel(model);
            if (!ModelState.IsValid)
            {
                return View("FaqForm", model);
            }

            var faq = await _db.FaqItems.FindAsync(id);
            if (faq == null) return NotFound();

            faq.Question = model.Question;
            faq.Answer = model.Answer;
            faq.Category = model.Category;
            faq.DisplayOrder = model.DisplayOrder;
            faq.IsActive = model.IsActive;
            faq.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            TempData["Success"] = "FAQ has been updated.";
            return RedirectToAction(nameof(Faqs));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFaq(int id)
        {
            var guard = SupportGuard(); if (guard != null) return guard;
            var faq = await _db.FaqItems.FindAsync(id);
            if (faq == null) return NotFound();

            _db.FaqItems.Remove(faq);
            await _db.SaveChangesAsync();
            TempData["Success"] = "FAQ has been deleted.";
            return RedirectToAction(nameof(Faqs));
        }

        public async Task<IActionResult> Inbox(int? id)
        {
            var guard = SupportGuard(); if (guard != null) return guard;

            var queries = await _db.ContactQueries
                .Include(q => q.Messages)
                .OrderByDescending(q => q.LastReplyAt ?? q.CreatedAt)
                .ToListAsync();

            var selectedQuery = id.HasValue
                ? queries.FirstOrDefault(q => q.Id == id.Value)
                : queries.FirstOrDefault();

            return View(new SupportInboxViewModel
            {
                Queries = queries,
                SelectedQuery = selectedQuery
            });
        }

        [HttpGet]
        public async Task<IActionResult> Conversation(int id)
        {
            var guard = SupportGuard(); if (guard != null) return guard;

            var query = await _db.ContactQueries
                .Include(q => q.Messages)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (query == null) return NotFound();

            return Json(new
            {
                id = query.Id,
                name = query.Name,
                email = query.Email,
                status = query.Status,
                replyUrl = Url.Action(nameof(Reply), "Support", new { id = query.Id }),
                messages = query.Messages
                    .OrderBy(m => m.SentAt)
                    .Select(m => new
                    {
                        senderType = m.SenderType,
                        message = m.Message,
                        sentAt = m.SentAt.ToLocalTime().ToString("dd MMM yyyy, hh:mm tt")
                    })
                    .ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, SupportInboxViewModel model)
        {
            var guard = SupportGuard(); if (guard != null) return guard;

            var query = await _db.ContactQueries
                .Include(q => q.Messages)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (query == null) return NotFound();

            var replyText = model.ReplyMessage.Trim();
            var now = DateTime.UtcNow;
            var support = await _db.EmpRegisters.FindAsync(CurrentSupportId);
            var supportName = support == null ? "Support Team" : $"{support.FirstName} {support.LastName}".Trim();

            query.Messages.Add(new ContactMessage
            {
                SenderType = "Staff",
                StaffId = CurrentSupportId,
                Message = replyText,
                SentAt = now
            });
            query.Status = "Replied";
            query.LastReplyAt = now;

            var log = new EmailNotificationLog
            {
                RecipientEmail = query.Email,
                RecipientName = query.Name,
                RecipientRole = "Visitor",
                Subject = "Reply from Health Insurance Support",
                EventType = "ContactQueryReply",
                RelatedEntityId = query.Id,
                Status = "Pending",
                CreatedAt = now
            };
            _db.EmailNotificationLogs.Add(log);

            try
            {
                await _emailSender.SendEmailAsync(
                    query.Email,
                    log.Subject,
                    BuildReplyEmail(query.Name, supportName, replyText));
                log.Status = "Sent";
                log.SentAt = DateTime.UtcNow;
                TempData["Success"] = "Reply sent to the visitor's email.";
            }
            catch (Exception ex)
            {
                log.Status = "Failed";
                log.ErrorMessage = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                TempData["Error"] = "Reply was saved, but the email could not be sent.";
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Inbox), new { id = query.Id });
        }

        public async Task<IActionResult> Details()
        {
            var guard = SupportGuard(); if (guard != null) return guard;
            var support = await _db.EmpRegisters.FindAsync(CurrentSupportId);
            if (support == null) return NotFound();
            return View("~/Views/Employee/Details.cshtml", support);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateDetails()
        {
            var guard = SupportGuard(); if (guard != null) return guard;
            var support = await _db.EmpRegisters.FindAsync(CurrentSupportId);
            if (support == null) return NotFound();
            return View("~/Views/Employee/UpdateDetails.cshtml", support);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDetails(EmpRegister model)
        {
            var guard = SupportGuard(); if (guard != null) return guard;
            ModelState.Remove("PasswordHash");
            ModelState.Remove("Username");

            if (!ModelState.IsValid)
                return View("~/Views/Employee/UpdateDetails.cshtml", model);

            var support = await _db.EmpRegisters.FindAsync(CurrentSupportId);
            if (support == null) return NotFound();

            support.Phone = model.Phone;
            support.Address = model.Address;
            support.Email = model.Email;
            support.Designation = model.Designation;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Your details have been updated.";
            return RedirectToAction(nameof(Details));
        }

        private static string BuildReplyEmail(string name, string supportName, string replyText)
        {
            return EmailTemplateBuilder.BuildSupportReplyEmail(name, supportName, replyText);
        }

        private static void PrepareFaqModel(FaqItem model)
        {
            model.Question = (model.Question ?? string.Empty).Trim();
            model.Answer = (model.Answer ?? string.Empty).Trim();
            model.Category = (model.Category ?? string.Empty).Trim();
        }
    }
}
