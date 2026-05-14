using System.ComponentModel.DataAnnotations;

namespace HealthInsuranceManagement.Models
{
    public class PasswordResetToken
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string UserType { get; set; } = string.Empty;

        public int UserId { get; set; }

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(128)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
