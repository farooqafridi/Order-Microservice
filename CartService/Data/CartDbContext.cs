using CartService.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace CartService.Data;

public sealed class CartDbContext : DbContext
{
    public CartDbContext(DbContextOptions<CartDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cart> Carts => Set<Cart>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("Carts");

            entity.HasKey(c => c.Id);

            //entity.HasIndex(c => c.CustomerId)
            //    .IsUnique();

            entity.Property(x => x.CustomerId)
           .HasMaxLength(450)
           .IsRequired();

            entity.HasIndex(x => x.CustomerId)
                .IsUnique();

            entity.Property(c => c.CreatedAtUtc)
                .IsRequired();

            entity.Property(c => c.UpdatedAtUtc)
                .IsRequired();

            entity.HasMany(c => c.Items)
                .WithOne(i => i.Cart)
                .HasForeignKey(i => i.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            // TotalAmount is computed in application code.
            entity.Ignore(c => c.TotalAmount);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems");

            entity.HasKey(i => i.Id);

            entity.Property(i => i.ProductName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(i => i.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(i => i.Quantity)
                .IsRequired();

            entity.Property(i => i.AddedAtUtc)
                .IsRequired();

            entity.HasIndex(i => new { i.CartId, i.ProductId })
                .IsUnique();

            entity.Ignore(i => i.LineTotal);
        });
    }
}