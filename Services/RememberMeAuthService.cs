using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HealthInsuranceManagement.Data;
using HealthInsuranceManagement.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HealthInsuranceManagement.Services
{
    public class RememberMeAuthService
    {
        private const string CookieName = "HIMS.RememberMe";
        private static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

        private readonly ApplicationDbContext _db;
        private readonly IDataProtector _protector;

        public RememberMeAuthService(ApplicationDbContext db, IDataProtectionProvider dataProtectionProvider)
        {
            _db = db;
            _protector = dataProtectionProvider.CreateProtector("HealthInsuranceManagement.RememberMeAuth.v1");
        }

        public void RememberAdmin(HttpContext httpContext, AdminLogin admin, bool rememberMe)
        {
            if (!rememberMe)
            {
                Forget(httpContext);
                return;
            }

            SetCookie(httpContext, new RememberMeTicket
            {
                UserType = UserRoles.Admin,
                UserId = admin.AdminId,
                PasswordFingerprint = Fingerprint(admin.PasswordHash),
                ExpiresAt = DateTimeOffset.UtcNow.Add(Lifetime)
            });
        }

        public void RememberStaff(HttpContext httpContext, EmpRegister employee, bool rememberMe)
        {
            if (!rememberMe)
            {
                Forget(httpContext);
                return;
            }

            SetCookie(httpContext, new RememberMeTicket
            {
                UserType = employee.Role,
                UserId = employee.EmpId,
                PasswordFingerprint = Fingerprint(employee.PasswordHash),
                ExpiresAt = DateTimeOffset.UtcNow.Add(Lifetime)
            });
        }

        public async Task TryRestoreSessionAsync(HttpContext httpContext)
        {
            if (!string.IsNullOrWhiteSpace(httpContext.Session.GetString("Role")))
                return;

            if (!httpContext.Request.Cookies.TryGetValue(CookieName, out var protectedTicket))
                return;

            RememberMeTicket? ticket;
            try
            {
                var json = _protector.Unprotect(protectedTicket);
                ticket = JsonSerializer.Deserialize<RememberMeTicket>(json);
            }
            catch
            {
                Forget(httpContext);
                return;
            }

            if (ticket == null || ticket.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                Forget(httpContext);
                return;
            }

            if (ticket.UserType == UserRoles.Admin)
            {
                var admin = await _db.AdminLogins.AsNoTracking().FirstOrDefaultAsync(a => a.AdminId == ticket.UserId);
                if (admin == null || !MatchesPassword(admin.PasswordHash, ticket.PasswordFingerprint))
                {
                    Forget(httpContext);
                    return;
                }

                httpContext.Session.SetInt32("UserId", admin.AdminId);
                httpContext.Session.SetString("Username", admin.Username);
                httpContext.Session.SetString("Role", UserRoles.Admin);
                return;
            }

            var employee = await _db.EmpRegisters.AsNoTracking().FirstOrDefaultAsync(e => e.EmpId == ticket.UserId);
            if (employee == null || !employee.IsActive || !UserRoles.StaffRoles.Contains(employee.Role) ||
                !MatchesPassword(employee.PasswordHash, ticket.PasswordFingerprint))
            {
                Forget(httpContext);
                return;
            }

            httpContext.Session.SetInt32("UserId", employee.EmpId);
            httpContext.Session.SetString("Username", employee.Username);
            httpContext.Session.SetString("FullName", $"{employee.FirstName} {employee.LastName}");
            httpContext.Session.SetString("Role", employee.Role);
        }

        public void Forget(HttpContext httpContext)
        {
            httpContext.Response.Cookies.Delete(CookieName);
        }

        private void SetCookie(HttpContext httpContext, RememberMeTicket ticket)
        {
            var json = JsonSerializer.Serialize(ticket);
            var protectedTicket = _protector.Protect(json);

            httpContext.Response.Cookies.Append(CookieName, protectedTicket, new CookieOptions
            {
                Expires = ticket.ExpiresAt,
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = httpContext.Request.IsHttps
            });
        }

        private static bool MatchesPassword(string passwordHash, string expectedFingerprint)
        {
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(Fingerprint(passwordHash)),
                Encoding.UTF8.GetBytes(expectedFingerprint));
        }

        private static string Fingerprint(string passwordHash)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash)));
        }

        private sealed class RememberMeTicket
        {
            public string UserType { get; set; } = string.Empty;
            public int UserId { get; set; }
            public string PasswordFingerprint { get; set; } = string.Empty;
            public DateTimeOffset ExpiresAt { get; set; }
        }
    }
}
