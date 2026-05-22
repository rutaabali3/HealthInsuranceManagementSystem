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

            var vm = new SupportDashboardViewModel
            {
                SupportUser = await _db.EmpRegisters.FindAsync(CurrentSupportId),
                NewQueries = await _db.ContactQueries.CountAsync(q => q.Status == "New"),
                OpenQueries = await _db.ContactQueries.CountAsync(q => q.Status != "Closed"),
                RepliedQueries = await _db.ContactQueries.CountAsync(q => q.Status == "Replied"),
                RecentQueries = await _db.ContactQueries
                    .OrderByDescending(q => q.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };

            return View(vm);
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
    }
}
