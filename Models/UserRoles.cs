namespace HealthInsuranceManagement.Models
{
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Employee = "Employee";
        public const string Manager = "Manager";
        public const string FinanceManager = "FinanceManager";

        public static readonly string[] StaffRoles = { Employee, Manager, FinanceManager };

        public static string DisplayName(string role) => role switch
        {
            FinanceManager => "Finance Manager",
            _ => role
        };
    }
}
