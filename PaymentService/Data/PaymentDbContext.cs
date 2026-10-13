using Microsoft.EntityFrameworkCore;
using PaymentService.Models;
using System.Reflection.Emit;

namespace PaymentService.Data;

public sealed class PaymentDbContext : DbContext
{
    public PaymentDbContext(
        DbContextOptions<PaymentDbContext> options)
        : base(options)
    {
    }

    public DbSet<PaymentTransaction> Payments =>
        Set<PaymentTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.OrderId)
                .IsUnique();

            entity.Property(x => x.PaymentId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2);
        });
    }
}