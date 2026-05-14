using System.ComponentModel.DataAnnotations;

namespace HealthInsuranceManagement.Models.ViewModels
{
    // ─── Authentication ───────────────────────────────────────────
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Display(Name = "New Password")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required, DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Display(Name = "New Password")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
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
    public class ReportViewModel
    {
        [DataType(DataType.Date)]
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        public List<EmpRegister> EmployeeReport { get; set; } = new();
        public List<PolicyRequestDetails> RequestReport { get; set; } = new();
        public List<PolicyBill> BillingReport { get; set; } = new();
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
