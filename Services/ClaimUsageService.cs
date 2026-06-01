using HealthInsuranceManagement.Data;
using HealthInsuranceManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthInsuranceManagement.Services
{
    public record ClaimUsageSummary(
        decimal CoverageLimit,
        decimal UsedAmount,
        decimal PendingAmount,
        decimal RemainingAmount,
        decimal AvailableAfterPending);

    public interface IClaimUsageService
    {
        Task<ClaimUsageSummary> GetUsageAsync(int policyOnEmployeeId, int? excludingClaimId = null);
        Task<bool> HasAvailableCoverageAsync(int policyOnEmployeeId, decimal claimAmount, int? excludingClaimId = null);
    }

    public class ClaimUsageService : IClaimUsageService
    {
        private static readonly string[] UsedStatuses =
        {
            InsuranceClaim.Approved,
            InsuranceClaim.Paid,
            InsuranceClaim.Closed
        };

        private readonly ApplicationDbContext _db;

        public ClaimUsageService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ClaimUsageSummary> GetUsageAsync(int policyOnEmployeeId, int? excludingClaimId = null)
        {
            var coverageLimit = await _db.PolicyOnEmployees
                .Where(pe => pe.Id == policyOnEmployeeId)
                .Select(pe => pe.Policy!.CoverageAmount)
                .FirstOrDefaultAsync();

            var claims = _db.InsuranceClaims
                .Where(c => c.PolicyOnEmployeeId == policyOnEmployeeId);

            if (excludingClaimId.HasValue)
                claims = claims.Where(c => c.ClaimId != excludingClaimId.Value);

            var usedAmount = await claims
                .Where(c => UsedStatuses.Contains(c.Status))
                .SumAsync(c => c.ApprovedAmount > 0 ? c.ApprovedAmount : c.ClaimAmount);

            var pendingAmount = await claims
                .Where(c => c.Status == InsuranceClaim.Pending)
                .SumAsync(c => c.ClaimAmount);

            var remainingAmount = Math.Max(coverageLimit - usedAmount, 0);
            var availableAfterPending = Math.Max(coverageLimit - usedAmount - pendingAmount, 0);

            return new ClaimUsageSummary(
                coverageLimit,
                usedAmount,
                pendingAmount,
                remainingAmount,
                availableAfterPending);
        }

        public async Task<bool> HasAvailableCoverageAsync(int policyOnEmployeeId, decimal claimAmount, int? excludingClaimId = null)
        {
            var usage = await GetUsageAsync(policyOnEmployeeId, excludingClaimId);
            return claimAmount > 0 && claimAmount <= usage.AvailableAfterPending;
        }
    }
}
