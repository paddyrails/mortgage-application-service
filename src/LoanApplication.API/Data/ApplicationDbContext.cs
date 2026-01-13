using Microsoft.EntityFrameworkCore;
using LoanApplication.API.Models;

namespace LoanApplication.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Application> Applications => Set<Application>();
    public DbSet<Underwriting> Underwritings => Set<Underwriting>();
    public DbSet<ApplicationDocument> Documents => Set<ApplicationDocument>();
    public DbSet<ApplicationCondition> Conditions => Set<ApplicationCondition>();
    public DbSet<ApplicationStatusHistory> StatusHistory => Set<ApplicationStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ApplicationNumber).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.PropertyId);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Underwriting)
                .WithOne(u => u.Application)
                .HasForeignKey<Underwriting>(u => u.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Documents)
                .WithOne(d => d.Application)
                .HasForeignKey(d => d.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Conditions)
                .WithOne(c => c.Application)
                .HasForeignKey(c => c.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.StatusHistory)
                .WithOne(h => h.Application)
                .HasForeignKey(h => h.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed sample data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var appId = Guid.Parse("55550000-0000-0000-0000-000000000001");
        var customerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var propertyId = Guid.Parse("aaaa1111-1111-1111-1111-111111111111");

        modelBuilder.Entity<Application>().HasData(
            new Application
            {
                Id = appId,
                ApplicationNumber = "APP-2024-000001",
                CustomerId = customerId,
                PropertyId = propertyId,
                RequestedLoanAmount = 680000,
                DownPaymentAmount = 170000,
                RequestedTermMonths = 360,
                LoanPurpose = LoanPurpose.Purchase,
                ApplicationType = ApplicationType.Purchase,
                Status = ApplicationStatus.Submitted,
                LTV = 80.0m,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                SubmittedAt = DateTime.UtcNow.AddDays(-9)
            }
        );
    }
}
