using System.ComponentModel.DataAnnotations;

namespace HealthInsuranceManagement.Models
{
    public class EmailNotificationLog
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string RecipientEmail { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? RecipientName { get; set; }

        [MaxLength(30)]
        public string? RecipientRole { get; set; }

        [Required, MaxLength(150)]
        public string Subject { get; set; } = string.Empty;

        [Required, MaxLength(80)]
        public string EventType { get; set; } = string.Empty;

        public int? RelatedEntityId { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SentAt { get; set; }
    }
}
