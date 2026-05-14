using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthInsuranceManagement.Models
{
    /// <summary>
    /// Tracks the approval/rejection of policy requests by a manager.
    /// </summary>
    public class PolicyApprovalDetails
    {
        [Key]
        public int ApprovalId { get; set; }

        [Required]
        public int RequestId { get; set; }

        [Required]
        public int ManagerId { get; set; }

        [Required, MaxLength(20)]
        public string Decision { get; set; } = "Pending"; // Pending, Approved, Rejected

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime DecisionDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("RequestId")]
        public PolicyRequestDetails? PolicyRequest { get; set; }

        [ForeignKey("ManagerId")]
        public EmpRegister? Manager { get; set; }
    }
}
