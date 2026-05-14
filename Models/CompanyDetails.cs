using System.ComponentModel.DataAnnotations;

namespace HealthInsuranceManagement.Models
{
    /// <summary>
    /// Stores insurance company information.
    /// </summary>
    public class CompanyDetails
    {
        [Key]
        public int CompanyId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Address { get; set; } = string.Empty;

        [Required, MaxLength(15)]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Website { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<Policy> Policies { get; set; } = new List<Policy>();
    }
}
