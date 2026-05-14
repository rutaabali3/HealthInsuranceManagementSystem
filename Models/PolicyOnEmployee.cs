using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthInsuranceManagement.Models
{
    /// <summary>
    /// Maps assigned policies to employees.
    /// </summary>
    public class PolicyOnEmployee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpId { get; set; }

        [Required]
        public int PolicyId { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
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
    }
}
