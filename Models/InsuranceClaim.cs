using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthInsuranceManagement.Models
{
    /// <summary>
    /// Stores real employee insurance claims against assigned policies.
    /// </summary>
    public class InsuranceClaim
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Paid = "Paid";
        public const string Closed = "Closed";

        [Key]
        public int ClaimId { get; set; }

        [Required]
        public int EmpId { get; set; }

        [Required]
        [Display(Name = "Assigned Policy")]
        public int PolicyOnEmployeeId { get; set; }

        [Required(ErrorMessage = "Claim type is required.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Claim type must be between 2 and 80 characters.")]
        [Display(Name = "Claim Type")]
        public string ClaimType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Claim amount is required.")]
        [Range(1, 999999999, ErrorMessage = "Claim amount must be greater than zero.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Claim Amount")]
        public decimal ClaimAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Approved Amount")]
        public decimal ApprovedAmount { get; set; }

        [Required(ErrorMessage = "Claim details are required.")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Claim details must be between 10 and 1000 characters.")]
        public string Reason { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Status { get; set; } = Pending;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime? ClosedAt { get; set; }

        public int? ManagerId { get; set; }

        public int? FinanceManagerId { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        [ForeignKey("EmpId")]
        public EmpRegister? Employee { get; set; }

        [ForeignKey("PolicyOnEmployeeId")]
        public PolicyOnEmployee? AssignedPolicy { get; set; }

        [ForeignKey("ManagerId")]
        public EmpRegister? Manager { get; set; }

        [ForeignKey("FinanceManagerId")]
        public EmpRegister? FinanceManager { get; set; }
    }
}
