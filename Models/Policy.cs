using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthInsuranceManagement.Models
{
    /// <summary>
    /// Stores insurance policy details linked to a company.
    /// </summary>
    public class Policy
    {
        [Key]
        public int PolicyId { get; set; }

        [Required]
        [Display(Name = "Insurance Company")]
        public int CompanyId { get; set; }

        [Required(ErrorMessage = "Policy name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Policy name must be at least 3 characters.")]
        [Display(Name = "Policy Name")]
        public string PolicyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Policy type is required.")]
        [MaxLength(50)]
        [Display(Name = "Policy Type")]
        public string PolicyType { get; set; } = string.Empty; // e.g., Individual, Family, Group

        [Required(ErrorMessage = "Premium amount is required.")]
        [Range(1, 999999999, ErrorMessage = "Premium amount must be greater than zero.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Premium Amount")]
        public decimal PremiumAmount { get; set; }

        [Required(ErrorMessage = "Coverage amount is required.")]
        [Range(1, 999999999, ErrorMessage = "Coverage amount must be greater than zero.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Coverage Amount")]
        public decimal CoverageAmount { get; set; }

        [Required(ErrorMessage = "Duration is required.")]
        [Range(1, 60, ErrorMessage = "Duration must be between 1 and 60 months.")]
        [Display(Name = "Duration (Months)")]
        public int DurationMonths { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? Benefits { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CompanyId")]
        public CompanyDetails? Company { get; set; }

        public ICollection<PolicyOnEmployee> PolicyOnEmployees { get; set; } = new List<PolicyOnEmployee>();
        public ICollection<PolicyTotalDescription> PolicyDescriptions { get; set; } = new List<PolicyTotalDescription>();
    }
}
