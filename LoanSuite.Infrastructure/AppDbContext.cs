using LoanSuite.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace LoanSuite.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // DbSets for each entity
        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<RepaymentSchedule> RepaymentSchedules { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Customer → Loan
            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Loans)
                .WithOne(l => l.Customer)
                .HasForeignKey(l => l.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Loan → RepaymentSchedules
            modelBuilder.Entity<Loan>()
                .HasMany(l => l.RepaymentSchedules)
                .WithOne(r => r.Loan)
                .HasForeignKey(r => r.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            // Loan → Payments
            modelBuilder.Entity<Loan>()
                .HasMany(l => l.Payments)
                .WithOne(p => p.Loan)
                .HasForeignKey(p => p.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ RepaymentSchedule ↔ Payment (one-to-one, no cascade to avoid multiple cascade paths)
            modelBuilder.Entity<RepaymentSchedule>()
                .HasOne(r => r.Payment)
                .WithOne(p => p.RepaymentSchedule)
                .HasForeignKey<Payment>(p => p.RepaymentScheduleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Additional configurations (indexes, default values, etc.)
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Email)
                .IsUnique();

            // ✅ Apply global precision to all decimals
            foreach (var property in modelBuilder.Model
                .GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }

            // Seed initial admin user for testing
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Email = "admin@loansuite.com",
                PasswordHash = "admin123", // ⚠️ hash this in production
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
