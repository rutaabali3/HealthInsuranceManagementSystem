using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthInsuranceManagement.Models
{
    /// <summary>
    /// Maps assigned policies to employees.
    /// </summary>
    public class PolicyOnEmployee : IValidatableObject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpId { get; set; }

        [Required(ErrorMessage = "Policy is required.")]
        public int PolicyId { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Expired, Cancelled

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Claimed Amount")]
        public decimal ClaimedAmount { get; set; } = 0;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("EmpId")]
        public EmpRegister? Employee { get; set; }

        [ForeignKey("PolicyId")]
        public Policy? Policy { get; set; }

        public ICollection<InsuranceClaim> InsuranceClaims { get; set; } = new List<InsuranceClaim>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate.Date < DateTime.Today)
                yield return new ValidationResult("Start date cannot be in the past.", new[] { nameof(StartDate) });

            if (EndDate.Date <= StartDate.Date)
                yield return new ValidationResult("End date must be after the start date.", new[] { nameof(EndDate) });
        }
    }
}
