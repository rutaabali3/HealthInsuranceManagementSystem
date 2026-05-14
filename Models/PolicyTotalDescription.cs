using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthInsuranceManagement.Models
{
    /// <summary>
    /// Stores full coverage breakdown and claim history for a policy.
    /// </summary>
    public class PolicyTotalDescription
    {
        [Key]
        public int DescId { get; set; }

        [Required]
        public int PolicyId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Coverage Type")]
        public string CoverageType { get; set; } = string.Empty; // e.g., Hospitalization, OPD, Dental

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Max Limit")]
        public decimal MaxLimit { get; set; }

        [MaxLength(500)]
        public string? Exclusions { get; set; }

        [MaxLength(500)]
        [Display(Name = "Claim Process")]
        public string? ClaimProcess { get; set; }

        // Navigation property
        [ForeignKey("PolicyId")]
        public Policy? Policy { get; set; }
    }
}
