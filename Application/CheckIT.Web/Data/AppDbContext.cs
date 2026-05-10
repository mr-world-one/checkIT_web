using CheckIT.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CheckIT.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<AnalysisHistory> AnalysisHistories { get; set; }
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UnblockRequest> UnblockRequests => Set<UnblockRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.IsBlocked).HasDefaultValue(false);
        });

        builder.Entity<UnblockRequest>(entity =>
        {
            entity.Property(x => x.Message).HasMaxLength(2000);
            entity.Property(x => x.AdminResponse).HasMaxLength(1000);
            entity.Property(x => x.Status).HasDefaultValue(UnblockRequestStatus.Open);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => new { x.Status, x.CreatedAtUtc });
        });
    }
}
