using System.ComponentModel.DataAnnotations;

namespace HealthInsuranceManagement.Models
{
    /// <summary>
    /// Stores hospital information for insurance claims.
    /// </summary>
    public class HospitalInfo
    {
        [Key]
        public int HospitalId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Hospital Name")]
        public string HospitalName { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Address { get; set; } = string.Empty;

        [Required, MaxLength(15)]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        [EmailAddress, MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        [MaxLength(500)]
        [Display(Name = "Services Offered")]
        public string? ServicesOffered { get; set; }

        public bool IsEmpanelled { get; set; } = true;
    }
}
