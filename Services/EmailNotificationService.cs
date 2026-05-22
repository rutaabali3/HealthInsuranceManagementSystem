using HealthInsuranceManagement.Data;
using HealthInsuranceManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthInsuranceManagement.Services
{
    public class EmailNotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;

        public EmailNotificationService(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

        public async Task NotifyAsync(
            string toEmail,
            string? toName,
            string? recipientRole,
            string subject,
            string htmlBody,
            string eventType,
            int? relatedEntityId = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                return;

            var log = new EmailNotificationLog
            {
                RecipientEmail = toEmail.Trim(),
                RecipientName = toName,
                RecipientRole = recipientRole,
                Subject = subject,
                EventType = eventType,
                RelatedEntityId = relatedEntityId,
                Status = "Pending"
            };

            _db.EmailNotificationLogs.Add(log);

            try
            {
                await _emailSender.SendEmailAsync(
                    log.RecipientEmail,
                    subject,
                    EmailTemplateBuilder.BuildNotificationEmail(
                        subject,
                        htmlBody,
                        toName,
                        recipientRole,
                        eventType,
                        relatedEntityId));
                log.Status = "Sent";
                log.SentAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                log.Status = "Failed";
                log.ErrorMessage = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            }

            await _db.SaveChangesAsync();
        }

        public async Task NotifyAdminsAsync(string subject, string htmlBody, string eventType, int? relatedEntityId = null)
        {
            var admins = await _db.AdminLogins.AsNoTracking().ToListAsync();

            foreach (var admin in admins)
            {
                await NotifyAsync(admin.Email, admin.Username, UserRoles.Admin, subject, htmlBody, eventType, relatedEntityId);
            }
        }

        public async Task NotifyStaffByRoleAsync(string role, string subject, string htmlBody, string eventType, int? relatedEntityId = null)
        {
            var staff = await _db.EmpRegisters
                .AsNoTracking()
                .Where(e => e.IsActive && e.Role == role)
                .ToListAsync();

            foreach (var user in staff)
            {
                await NotifyAsync(user.Email, $"{user.FirstName} {user.LastName}", role, subject, htmlBody, eventType, relatedEntityId);
            }
        }

        public async Task NotifyAllStaffAsync(string subject, string htmlBody, string eventType, int? relatedEntityId = null)
        {
            var staff = await _db.EmpRegisters
                .AsNoTracking()
                .Where(e => e.IsActive)
                .ToListAsync();

            foreach (var user in staff)
            {
                await NotifyAsync(user.Email, $"{user.FirstName} {user.LastName}", user.Role, subject, htmlBody, eventType, relatedEntityId);
            }
        }

        public async Task NotifyCompanyAsync(CompanyDetails? company, string subject, string htmlBody, string eventType, int? relatedEntityId = null)
        {
            if (company == null || string.IsNullOrWhiteSpace(company.Email))
                return;

            await NotifyAsync(company.Email, company.CompanyName, "Company", subject, htmlBody, eventType, relatedEntityId);
        }

    }
}
