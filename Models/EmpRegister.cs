using System.ComponentModel.DataAnnotations;

namespace HealthInsuranceManagement.Models
{
    /// <summary>
    /// Stores employee registration and login details.
    /// </summary>
    public class EmpRegister
    {
        [Key]
        public int EmpId { get; set; }

        [Required, MaxLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(15)]
        [Display(Name = "Phone")]
        public string Phone { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Department { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Designation { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Role { get; set; } = UserRoles.Employee;

        [Required]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required, MaxLength(10)]
        public string Gender { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<PolicyOnEmployee> PolicyOnEmployees { get; set; } = new List<PolicyOnEmployee>();
        public ICollection<PolicyRequestDetails> PolicyRequests { get; set; } = new List<PolicyRequestDetails>();
    }
}
