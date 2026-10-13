using InventoryService.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace InventoryService.Data;

public sealed class InventoryDbContext : DbContext
{
    public InventoryDbContext(
        DbContextOptions<InventoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<InventoryItem> InventoryItems =>
        Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ProductName)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => x.ProductId)
                .IsUnique();

            entity.HasIndex(x => x.ProductName)
                .IsUnique();

            entity.Property(x => x.RowVersion)
                .IsRowVersion();

            entity.ToTable(table =>
                table.HasCheckConstraint(
                    "CK_InventoryItems_AvailableQuantity",
                    "[AvailableQuantity] >= 0"));
        });
    }
}