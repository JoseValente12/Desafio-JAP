using JapCarRental.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace JapCarRental.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<RentalContract> RentalContracts => Set<RentalContract>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vehicle>(e =>
        {
            e.Property(v => v.Brand).IsRequired().HasMaxLength(50);
            e.Property(v => v.Model).IsRequired().HasMaxLength(50);
            e.Property(v => v.LicensePlate).IsRequired().HasMaxLength(10);
            e.Property(v => v.FuelType).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(v => v.LicensePlate).IsUnique();
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.Property(c => c.FullName).IsRequired().HasMaxLength(150);
            e.Property(c => c.Email).IsRequired().HasMaxLength(254);
            e.Property(c => c.PhoneNumber).IsRequired().HasMaxLength(20);
            e.Property(c => c.DrivingLicenseNumber).IsRequired().HasMaxLength(30);
            e.HasIndex(c => c.Email).IsUnique();
        });

        modelBuilder.Entity<RentalContract>(e =>
        {
            e.HasOne(r => r.Customer)
                .WithMany(c => c.Contracts)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.Vehicle)
                .WithMany(v => v.Contracts)
                .HasForeignKey(r => r.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(r => new { r.VehicleId, r.StartDate, r.EndDate });
        });
    }
}