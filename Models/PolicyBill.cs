using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthInsuranceManagement.Models
{
    /// <summary>
    /// Tracks the bill/payment workflow for a policy request.
    /// </summary>
    public class PolicyBill
    {
        [Key]
        public int BillId { get; set; }

        [Required]
        public int RequestId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Bill Amount")]
        public decimal Amount { get; set; }

        [MaxLength(30)]
        public string Status { get; set; } = "Created";

        public int? ManagerId { get; set; }

        public DateTime? ManagerDecisionAt { get; set; }

        public DateTime? ForwardedAt { get; set; }

        public int? FinanceManagerId { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime? ClosedAt { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("RequestId")]
        public PolicyRequestDetails? PolicyRequest { get; set; }

        [ForeignKey("ManagerId")]
        public EmpRegister? Manager { get; set; }

        [ForeignKey("FinanceManagerId")]
        public EmpRegister? FinanceManager { get; set; }
    }
}
