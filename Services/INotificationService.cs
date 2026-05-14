namespace HealthInsuranceManagement.Services
{
    public interface INotificationService
    {
        Task NotifyAsync(
            string toEmail,
            string? toName,
            string? recipientRole,
            string subject,
            string htmlBody,
            string eventType,
            int? relatedEntityId = null);

        Task NotifyAdminsAsync(string subject, string htmlBody, string eventType, int? relatedEntityId = null);

        Task NotifyStaffByRoleAsync(string role, string subject, string htmlBody, string eventType, int? relatedEntityId = null);
    }
}
