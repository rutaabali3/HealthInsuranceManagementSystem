using System.ComponentModel.DataAnnotations;

namespace HealthInsuranceManagement.Models
{
    /// <summary>
    /// Stores administrator login credentials.
    /// </summary>
    public class AdminLogin
    {
        [Key]
        public int AdminId { get; set; }

        [Required, MaxLength(50)]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
