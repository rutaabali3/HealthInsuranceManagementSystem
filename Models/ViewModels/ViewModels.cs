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

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
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

    public class ContactFormViewModel
    {
        [Required(ErrorMessage = "Your name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Your name must be between 3 and 100 characters.")]
        [RegularExpression(@"^[A-Za-z][A-Za-z\s'-]*$", ErrorMessage = "Your name cannot contain numbers or special characters.")]
        [Display(Name = "Your Name")]
        public string ContactName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email address cannot be longer than 150 characters.")]
        [Display(Name = "Email Address")]
        public string ContactEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Message must be between 10 and 2000 characters.")]
        [Display(Name = "Message")]
        public string ContactMessage { get; set; } = string.Empty;
    }

    public class FaqPageViewModel
    {
        public List<FaqItem> Items { get; set; } = new();

        public IEnumerable<IGrouping<string, FaqItem>> GroupedItems =>
            Items
                .OrderBy(f => f.Category)
                .ThenBy(f => f.DisplayOrder)
                .ThenBy(f => f.Question)
                .GroupBy(f => f.Category);
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
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public decimal PaidClaimAmountThisMonth { get; set; }
        public decimal ApprovedClaimAmountThisMonth { get; set; }
        public int BillsGeneratedThisMonth { get; set; }
        public List<DashboardTrendPoint> OverviewTrend { get; set; } = new();
        public List<PolicyRequestDetails> RecentRequests { get; set; } = new();
        public List<EmpRegister> RecentEmployees { get; set; } = new();
        public List<CompanyDetails> RecentCompanies { get; set; } = new();
        public List<Policy> RecentPolicies { get; set; } = new();
        public List<InsuranceClaim> RecentClaims { get; set; } = new();
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
        public List<InsuranceClaim> RecentClaims { get; set; } = new();
    }

    public class ClaimUsageViewModel
    {
        public int PolicyOnEmployeeId { get; set; }
        public string PolicyName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal CoverageLimit { get; set; }
        public decimal UsedAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal AvailableAfterPending { get; set; }
    }

    public class EmployeeClaimsViewModel
    {
        public List<ClaimUsageViewModel> PolicyUsage { get; set; } = new();
        public List<InsuranceClaim> Claims { get; set; } = new();
    }

    public class SubmitClaimViewModel
    {
        [Required(ErrorMessage = "Assigned policy is required.")]
        [Display(Name = "Assigned Policy")]
        public int PolicyOnEmployeeId { get; set; }

        [Required(ErrorMessage = "Claim type is required.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Claim type must be between 2 and 80 characters.")]
        [Display(Name = "Claim Type")]
        public string ClaimType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Claim amount is required.")]
        [Range(1, 999999999, ErrorMessage = "Claim amount must be greater than zero.")]
        [Display(Name = "Claim Amount")]
        public decimal ClaimAmount { get; set; }

        [Required(ErrorMessage = "Claim details are required.")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Claim details must be between 10 and 1000 characters.")]
        [Display(Name = "Claim Details")]
        public string Reason { get; set; } = string.Empty;

        public List<ClaimUsageViewModel> PolicyUsage { get; set; } = new();
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
        public List<InsuranceClaim> ClaimReport { get; set; } = new();

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
        public List<InsuranceClaim> Claims { get; set; } = new();
    }

    public class ManagerDashboardViewModel
    {
        public EmpRegister? Manager { get; set; }
        public List<PolicyBill> ReceivedBills { get; set; } = new();
        public List<InsuranceClaim> PendingClaims { get; set; } = new();
    }

    public class FinanceDashboardViewModel
    {
        public EmpRegister? FinanceManager { get; set; }
        public List<PolicyBill> ReceivedBills { get; set; } = new();
        public List<InsuranceClaim> ReceivedClaims { get; set; } = new();
    }

    public class SupportDashboardViewModel
    {
        public EmpRegister? SupportUser { get; set; }
        public int NewQueries { get; set; }
        public int OpenQueries { get; set; }
        public int RepliedQueries { get; set; }
        public int ActiveFaqs { get; set; }
        public List<ContactQuery> RecentQueries { get; set; } = new();
    }

    public class SupportInboxViewModel
    {
        public List<ContactQuery> Queries { get; set; } = new();
        public ContactQuery? SelectedQuery { get; set; }

        [Display(Name = "Reply")]
        public string ReplyMessage { get; set; } = string.Empty;
    }

    public class SupportFaqListViewModel
    {
        public List<FaqItem> Faqs { get; set; } = new();
        public int ActiveCount { get; set; }
        public int HiddenCount { get; set; }
    }
}
