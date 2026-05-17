using System.ComponentModel.DataAnnotations;
using HealthInsuranceManagement.Models;

namespace HealthInsuranceManagement.Models.ViewModels
{
    // ─── Authentication ───────────────────────────────────────────
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        [StrongPassword]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        [StrongPassword]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ─── Dashboard summary ────────────────────────────────────────
    public class AdminDashboardViewModel
    {
        public int TotalEmployees { get; set; }
        public int TotalCompanies { get; set; }
        public int TotalPolicies { get; set; }
        public int PendingRequests { get; set; }
        public int PendingBills { get; set; }
        public int NewEmployeesThisMonth { get; set; }
        public int NewPoliciesThisMonth { get; set; }
        public int ClaimsProcessedThisMonth { get; set; }
        public int BillsGeneratedThisMonth { get; set; }
        public List<DashboardTrendPoint> OverviewTrend { get; set; } = new();
        public List<PolicyRequestDetails> RecentRequests { get; set; } = new();
        public List<EmpRegister> RecentEmployees { get; set; } = new();
        public List<CompanyDetails> RecentCompanies { get; set; } = new();
        public List<Policy> RecentPolicies { get; set; } = new();
    }

    public class DashboardTrendPoint
    {
        public DateTime Date { get; set; }
        public int Value { get; set; }
    }

    public class EmployeeDashboardViewModel
    {
        public EmpRegister? Employee { get; set; }
        public List<PolicyOnEmployee> AssignedPolicies { get; set; } = new();
        public List<PolicyRequestDetails> MyRequests { get; set; } = new();
    }

    // ─── Policy search ────────────────────────────────────────────
    public class PolicySearchViewModel
    {
        public string? PolicyType { get; set; }
        public decimal? MinCoverage { get; set; }
        public decimal? MaxPremium { get; set; }
        public List<Policy> Results { get; set; } = new();
    }

    // ─── Reports ──────────────────────────────────────────────────
    public class ReportViewModel : IValidatableObject
    {
        [DataType(DataType.Date)]
        [Display(Name = "From Date")]
        [NotFutureDate(ErrorMessage = "From date cannot be in the future.")]
        public DateTime? FromDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "To Date")]
        [NotFutureDate(ErrorMessage = "To date cannot be in the future.")]
        public DateTime? ToDate { get; set; }

        public List<EmpRegister> EmployeeReport { get; set; } = new();
        public List<PolicyRequestDetails> RequestReport { get; set; } = new();
        public List<PolicyBill> BillingReport { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (FromDate.HasValue && ToDate.HasValue && FromDate.Value.Date > ToDate.Value.Date)
                yield return new ValidationResult("From date cannot be later than to date.", new[] { nameof(FromDate), nameof(ToDate) });
        }
    }

    public class AdminSearchViewModel
    {
        public string? Query { get; set; }
        public List<EmpRegister> Employees { get; set; } = new();
        public List<CompanyDetails> Companies { get; set; } = new();
        public List<Policy> Policies { get; set; } = new();
        public List<PolicyRequestDetails> Requests { get; set; } = new();
        public List<PolicyBill> Bills { get; set; } = new();
    }

    public class ManagerDashboardViewModel
    {
        public EmpRegister? Manager { get; set; }
        public List<PolicyBill> ReceivedBills { get; set; } = new();
    }

    public class FinanceDashboardViewModel
    {
        public EmpRegister? FinanceManager { get; set; }
        public List<PolicyBill> ReceivedBills { get; set; } = new();
    }
}
