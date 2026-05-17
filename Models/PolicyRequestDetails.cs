using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthInsuranceManagement.Models
{
    /// <summary>
    /// Stores employee requests for specific insurance policies.
    /// </summary>
    public class PolicyRequestDetails
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        public int EmpId { get; set; }

        [Required]
        public int PolicyId { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        [Required(ErrorMessage = "Bill amount is required.")]
        [Range(1, 999999999, ErrorMessage = "Bill amount must be greater than zero.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Bill Amount")]
        public decimal BillAmount { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, ApprovedByManager, ForwardedToFinance, Paid, Closed, Rejected

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("EmpId")]
        public EmpRegister? Employee { get; set; }

        [ForeignKey("PolicyId")]
        public Policy? Policy { get; set; }

        public PolicyApprovalDetails? Approval { get; set; }

        public PolicyBill? Bill { get; set; }
    }
}
