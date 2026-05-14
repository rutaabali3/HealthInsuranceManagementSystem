using Microsoft.EntityFrameworkCore;
using HealthInsuranceManagement.Models;

namespace HealthInsuranceManagement.Data
{
    /// <summary>
    /// Entity Framework Core database context for the Health Insurance system.
    /// All 9 tables from the specification are registered here.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ── DbSets (one per table) ─────────────────────────────────────
        public DbSet<AdminLogin> AdminLogins { get; set; }
        public DbSet<CompanyDetails> CompanyDetails { get; set; }
        public DbSet<EmpRegister> EmpRegisters { get; set; }
        public DbSet<HospitalInfo> HospitalInfos { get; set; }
        public DbSet<Policy> Policies { get; set; }
        public DbSet<PolicyOnEmployee> PolicyOnEmployees { get; set; }
        public DbSet<PolicyApprovalDetails> PolicyApprovalDetails { get; set; }
        public DbSet<PolicyBill> PolicyBills { get; set; }
        public DbSet<PolicyRequestDetails> PolicyRequestDetails { get; set; }
        public DbSet<PolicyTotalDescription> PolicyTotalDescriptions { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<EmailNotificationLog> EmailNotificationLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Unique constraints ─────────────────────────────────────
            modelBuilder.Entity<AdminLogin>()
                .HasIndex(a => a.Username).IsUnique();

            modelBuilder.Entity<EmpRegister>()
                .HasIndex(e => e.Username).IsUnique();

            modelBuilder.Entity<EmpRegister>()
                .HasIndex(e => e.Email).IsUnique();

            modelBuilder.Entity<PasswordResetToken>()
                .HasIndex(t => t.TokenHash).IsUnique();

            modelBuilder.Entity<PasswordResetToken>()
                .HasIndex(t => new { t.UserType, t.UserId, t.UsedAt });

            modelBuilder.Entity<EmailNotificationLog>()
                .HasIndex(n => new { n.RecipientEmail, n.EventType, n.CreatedAt });

            // ── Relationships ──────────────────────────────────────────
            modelBuilder.Entity<Policy>()
                .HasOne(p => p.Company)
                .WithMany(c => c.Policies)
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PolicyOnEmployee>()
                .HasOne(pe => pe.Employee)
                .WithMany(e => e.PolicyOnEmployees)
                .HasForeignKey(pe => pe.EmpId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PolicyOnEmployee>()
                .HasOne(pe => pe.Policy)
                .WithMany(p => p.PolicyOnEmployees)
                .HasForeignKey(pe => pe.PolicyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PolicyRequestDetails>()
                .HasOne(pr => pr.Employee)
                .WithMany(e => e.PolicyRequests)
                .HasForeignKey(pr => pr.EmpId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PolicyRequestDetails>()
                .HasOne(pr => pr.Policy)
                .WithMany()
                .HasForeignKey(pr => pr.PolicyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PolicyApprovalDetails>()
                .HasOne(pa => pa.PolicyRequest)
                .WithOne(pr => pr.Approval)
                .HasForeignKey<PolicyApprovalDetails>(pa => pa.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PolicyApprovalDetails>()
                .HasOne(pa => pa.Manager)
                .WithMany()
                .HasForeignKey(pa => pa.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PolicyBill>()
                .HasOne(b => b.PolicyRequest)
                .WithOne(r => r.Bill)
                .HasForeignKey<PolicyBill>(b => b.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PolicyBill>()
                .HasOne(b => b.Manager)
                .WithMany()
                .HasForeignKey(b => b.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PolicyBill>()
                .HasOne(b => b.FinanceManager)
                .WithMany()
                .HasForeignKey(b => b.FinanceManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PolicyTotalDescription>()
                .HasOne(ptd => ptd.Policy)
                .WithMany(p => p.PolicyDescriptions)
                .HasForeignKey(ptd => ptd.PolicyId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Seed default admin ─────────────────────────────────────
        }
    }
}
