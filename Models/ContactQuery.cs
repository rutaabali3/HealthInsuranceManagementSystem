using System.ComponentModel.DataAnnotations;

namespace HealthInsuranceManagement.Models
{
    public class ContactQuery
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Status { get; set; } = "New";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastReplyAt { get; set; }

        public ICollection<ContactMessage> Messages { get; set; } = new List<ContactMessage>();
    }

    public class ContactMessage
    {
        [Key]
        public int Id { get; set; }

        public int ContactQueryId { get; set; }

        [Required, MaxLength(20)]
        public string SenderType { get; set; } = "Visitor";

        public int? StaffId { get; set; }

        [Required, MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public ContactQuery? ContactQuery { get; set; }
    }
}
