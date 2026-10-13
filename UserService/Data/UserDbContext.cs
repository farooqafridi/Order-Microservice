using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using UserService.Models;

namespace UserService.Data;

public sealed class UserDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public UserDbContext(
        DbContextOptions<UserDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.DisplayName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(u => u.CreatedAtUtc)
                .IsRequired();
        });
    }
}