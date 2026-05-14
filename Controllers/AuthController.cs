using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using HealthInsuranceManagement.Data;
using HealthInsuranceManagement.Models;
using HealthInsuranceManagement.Models.ViewModels;
using HealthInsuranceManagement.Services;

namespace HealthInsuranceManagement.Controllers
{
    /// <summary>
    /// Handles authentication for Admin, Employee, Manager, and Finance Manager.
    /// </summary>
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;

        public AuthController(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login()
        {
            // Redirect already-logged-in users
            var role = HttpContext.Session.GetString("Role");
            if (role == UserRoles.Admin) return RedirectToAction("Dashboard", "Admin");
            if (role == UserRoles.Employee) return RedirectToAction("Dashboard", "Employee");
            if (role == UserRoles.Manager) return RedirectToAction("Dashboard", "Manager");
            if (role == UserRoles.FinanceManager) return RedirectToAction("Dashboard", "Finance");

            return View(new LoginViewModel());
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var admin = await _db.AdminLogins
                .FirstOrDefaultAsync(a => a.Username == model.Username);

            if (admin != null && BCrypt.Net.BCrypt.Verify(model.Password, admin.PasswordHash))
            {
                HttpContext.Session.SetInt32("UserId", admin.AdminId);
                HttpContext.Session.SetString("Username", admin.Username);
                HttpContext.Session.SetString("Role", UserRoles.Admin);
                return RedirectToAction("Dashboard", "Admin");
            }

            var emp = await _db.EmpRegisters
                .FirstOrDefaultAsync(e => e.Username == model.Username && e.IsActive);

            if (emp == null || !BCrypt.Net.BCrypt.Verify(model.Password, emp.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            if (!UserRoles.StaffRoles.Contains(emp.Role))
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            HttpContext.Session.SetInt32("UserId", emp.EmpId);
            HttpContext.Session.SetString("Username", emp.Username);
            HttpContext.Session.SetString("FullName", $"{emp.FirstName} {emp.LastName}");
            HttpContext.Session.SetString("Role", emp.Role);

            return emp.Role switch
            {
                UserRoles.Manager => RedirectToAction("Dashboard", "Manager"),
                UserRoles.FinanceManager => RedirectToAction("Dashboard", "Finance"),
                _ => RedirectToAction("Dashboard", "Employee")
            };
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim();
            var admin = await _db.AdminLogins.FirstOrDefaultAsync(a => a.Email == email);
            var employee = admin == null
                ? await _db.EmpRegisters.FirstOrDefaultAsync(e => e.Email == email && e.IsActive)
                : null;

            if (admin != null || employee != null)
            {
                var userType = admin != null ? UserRoles.Admin : employee!.Role;
                var userId = admin?.AdminId ?? employee!.EmpId;

                var oldTokens = await _db.PasswordResetTokens
                    .Where(t => t.UserType == userType && t.UserId == userId && t.UsedAt == null)
                    .ToListAsync();

                foreach (var oldToken in oldTokens)
                    oldToken.UsedAt = DateTime.UtcNow;

                var token = CreateResetToken();
                _db.PasswordResetTokens.Add(new PasswordResetToken
                {
                    UserType = userType,
                    UserId = userId,
                    Email = email,
                    TokenHash = HashResetToken(token),
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30)
                });

                await _db.SaveChangesAsync();

                var resetUrl = Url.Action("ResetPassword", "Auth", new { token }, Request.Scheme)
                    ?? Url.Action("ResetPassword", "Auth", new { token })!;

                var body = $"""
                    <h2>Password reset request</h2>
                    <p>Use the button below to reset your Health Insurance Management password.</p>
                    <p><a href="{resetUrl}" style="display:inline-block;padding:12px 18px;background:#006a6a;color:#ffffff;text-decoration:none;border-radius:6px;">Reset Password</a></p>
                    <p>This link expires in 30 minutes.</p>
                    <p>If the button does not work, copy this link into your browser:<br>{resetUrl}</p>
                    """;

                try
                {
                    await _emailSender.SendEmailAsync(email, "Reset your Health Insurance Management password", body);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Unable to send reset email: {ex.Message}");
                    return View(model);
                }
            }

            TempData["Success"] = "If that email exists, a password reset link has been sent.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || !await IsValidResetTokenAsync(token))
            {
                TempData["Error"] = "This password reset link is invalid or expired.";
                return RedirectToAction(nameof(Login));
            }

            return View(new ResetPasswordViewModel { Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var tokenHash = HashResetToken(model.Token);
            var resetToken = await _db.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow);

            if (resetToken == null)
            {
                TempData["Error"] = "This password reset link is invalid or expired.";
                return RedirectToAction(nameof(Login));
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            if (resetToken.UserType == UserRoles.Admin)
            {
                var admin = await _db.AdminLogins.FindAsync(resetToken.UserId);
                if (admin == null)
                {
                    TempData["Error"] = "This password reset link is invalid or expired.";
                    return RedirectToAction(nameof(Login));
                }

                admin.PasswordHash = passwordHash;
            }
            else
            {
                var employee = await _db.EmpRegisters.FindAsync(resetToken.UserId);
                if (employee == null || !employee.IsActive || !UserRoles.StaffRoles.Contains(employee.Role))
                {
                    TempData["Error"] = "This password reset link is invalid or expired.";
                    return RedirectToAction(nameof(Login));
                }

                employee.PasswordHash = passwordHash;
            }

            resetToken.UsedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Password reset successfully. You can log in with your new password.";
            return RedirectToAction(nameof(Login));
        }

        // GET: /Auth/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private async Task<bool> IsValidResetTokenAsync(string token)
        {
            var tokenHash = HashResetToken(token);
            return await _db.PasswordResetTokens
                .AnyAsync(t => t.TokenHash == tokenHash && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow);
        }

        private static string CreateResetToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        private static string HashResetToken(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }
    }
}
