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

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "First name must be at least 3 characters.")]
        [RegularExpression(@"^[A-Za-z][A-Za-z\s'-]*$", ErrorMessage = "First name can contain letters, spaces, apostrophes, and hyphens only.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Last name must be at least 3 characters.")]
        [RegularExpression(@"^[A-Za-z][A-Za-z\s'-]*$", ErrorMessage = "Last name can contain letters, spaces, apostrophes, and hyphens only.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be at least 3 characters.")]
        [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Username can contain letters, numbers, dots, underscores, and hyphens only.")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^(\+92|0)\d{10}$", ErrorMessage = "Enter a Pakistan phone number starting with +92 or 0, using 11 digits after 0.")]
        [MaxLength(13)]
        [Display(Name = "Phone")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Department must be at least 2 characters.")]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "Designation is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Designation must be at least 2 characters.")]
        public string Designation { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Role { get; set; } = UserRoles.Employee;

        [Required(ErrorMessage = "Date of birth is required.")]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        [AdultDate(MinimumAge = 18, ErrorMessage = "Employee must be at least 18 years old.")]
        public DateTime DateOfBirth { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Gender is required.")]
        [MaxLength(10)]
        public string Gender { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<PolicyOnEmployee> PolicyOnEmployees { get; set; } = new List<PolicyOnEmployee>();
        public ICollection<PolicyRequestDetails> PolicyRequests { get; set; } = new List<PolicyRequestDetails>();
        public ICollection<InsuranceClaim> InsuranceClaims { get; set; } = new List<InsuranceClaim>();
    }
}
