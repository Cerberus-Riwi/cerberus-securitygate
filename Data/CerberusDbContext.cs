using cerberus_securitygate.Models;
using Microsoft.EntityFrameworkCore;

namespace cerberus_securitygate.Data;

public class CerberusDbContext : DbContext
{
    public CerberusDbContext(DbContextOptions<CerberusDbContext> options) : base(options) { }

    public DbSet<ScanRequest> ScanRequests => Set<ScanRequest>();
    public DbSet<ScanResult> ScanResults => Set<ScanResult>();
    public DbSet<Finding> Findings => Set<Finding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScanRequest>(entity =>
        {
            entity.HasKey(e => e.ScanId);
            entity.Property(e => e.ScanId).ValueGeneratedNever();
            entity.Property(e => e.ReceivedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<ScanResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ReceivedAt).HasDefaultValueSql("now()");
            entity.HasMany(r => r.Findings)
                  .WithOne()
                  .HasForeignKey(f => f.ScanResultId);
        });

        modelBuilder.Entity<Finding>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
        });
    }
}