using Microsoft.EntityFrameworkCore;
using HealthInsuranceManagement.Models;

namespace HealthInsuranceManagement.Data
{
    /// <summary>
    /// Applies database migrations and guarantees the default users exist.
    /// </summary>
    public static class DatabaseInitializer
    {
        private const string DefaultAdminUsername = "admin";
        private const string DefaultAdminPassword = "Admin@123";

        public static async Task InitializeAsync(IServiceProvider services)
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await db.Database.MigrateAsync();

            var admin = await db.AdminLogins
                .FirstOrDefaultAsync(existingAdmin => existingAdmin.Username == DefaultAdminUsername);

            if (admin is null)
            {
                db.AdminLogins.Add(new AdminLogin
                {
                    AdminId = 1,
                    Username = DefaultAdminUsername,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultAdminPassword),
                    Email = "admin@healthinsurance.com",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });

                await db.SaveChangesAsync();
            }
            else if (!BCrypt.Net.BCrypt.Verify(DefaultAdminPassword, admin.PasswordHash))
            {
                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultAdminPassword);
                await db.SaveChangesAsync();
            }

            await SeedStaffUserAsync(db, "manager", "Manager@123", "System", "Manager", UserRoles.Manager, "manager@healthinsurance.com");
            await SeedStaffUserAsync(db, "finance", "Finance@123", "Finance", "Manager", UserRoles.FinanceManager, "finance@healthinsurance.com");
            await SeedStaffUserAsync(db, "support", "Support@123", "Support", "Agent", UserRoles.Support, "support@healthinsurance.com");
        }

        private static async Task SeedStaffUserAsync(
            ApplicationDbContext db,
            string username,
            string password,
            string firstName,
            string lastName,
            string role,
            string email)
        {
            var user = await db.EmpRegisters.FirstOrDefaultAsync(e => e.Username == username);
            if (user is null)
            {
                db.EmpRegisters.Add(new EmpRegister
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Username = username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    Email = email,
                    Phone = "0000000000",
                    Department = role switch
                    {
                        UserRoles.Manager => "Management",
                        UserRoles.FinanceManager => "Finance",
                        UserRoles.Support => "Support",
                        _ => "Operations"
                    },
                    Designation = UserRoles.DisplayName(role),
                    Role = role,
                    DateOfBirth = new DateTime(1990, 1, 1),
                    Gender = "Other",
                    Address = "System generated user"
                });
                await db.SaveChangesAsync();
                return;
            }

            user.Role = role;
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            }
            await db.SaveChangesAsync();
        }
    }
}
