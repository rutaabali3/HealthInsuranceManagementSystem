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

        [Required, MaxLength(100)]
        [Display(Name = "Policy Name")]
        public string PolicyName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        [Display(Name = "Policy Type")]
        public string PolicyType { get; set; } = string.Empty; // e.g., Individual, Family, Group

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Premium Amount")]
        public decimal PremiumAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Coverage Amount")]
        public decimal CoverageAmount { get; set; }

        [Required]
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
