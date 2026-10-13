using CategoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace CategoryService.Data;

public sealed class CategoryDbContext : DbContext
{
    public CategoryDbContext(
        DbContextOptions<CategoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(c => c.Description)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(c => c.IsActive)
                .IsRequired();

            entity.Property(c => c.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(c => c.Name)
                .IsUnique();

            entity.HasIndex(c => c.IsActive);
        });
    }
}