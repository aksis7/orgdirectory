using Microsoft.EntityFrameworkCore;
using OrgDirectory.Web.Models;

namespace OrgDirectory.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Citizen> Citizens => Set<Citizen>();
    public DbSet<Organization> Organizations => Set<Organization>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Activity>()
            .HasIndex(a => a.Name)
            .IsUnique();

        modelBuilder.Entity<Organization>()
            .Property(o => o.CharterCapital)
            .HasColumnType("numeric(18,2)");

        modelBuilder.Entity<Organization>()
            .HasOne(o => o.Activity)
            .WithMany(a => a.Organizations)
            .HasForeignKey(o => o.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Organization>()
            .HasOne(o => o.Director)
            .WithMany(c => c.OrganizationsDirected)
            .HasForeignKey(o => o.DirectorId)
            .OnDelete(DeleteBehavior.Restrict);

            }
}
