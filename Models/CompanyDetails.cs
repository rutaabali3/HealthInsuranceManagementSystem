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

        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Company name must be at least 3 characters.")]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Address must be at least 5 characters.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required.")]
        [RegularExpression(@"^(\+92|0)\d{10}$", ErrorMessage = "Enter a Pakistan contact number starting with +92 or 0, using 11 digits after 0.")]
        [MaxLength(13)]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Url(ErrorMessage = "Enter a valid website URL.")]
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
