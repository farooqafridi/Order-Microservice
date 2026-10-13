using Microsoft.EntityFrameworkCore;
using OrderService.Models;
using System.Reflection.Emit;

namespace OrderService.Data;

public sealed class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.CustomerId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(x => x.IdempotencyKey)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.CustomerName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.CustomerId,
                x.IdempotencyKey
            })
            .IsUnique();

            entity.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey("OrderId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(x => x.TotalAmount);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ProductName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            entity.Ignore(x => x.LineTotal);
        });
    }
}