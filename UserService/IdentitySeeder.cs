using Microsoft.AspNetCore.Identity;
using UserService.Models;

namespace UserService;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(
        IServiceProvider services)
    {
        var roleManager =
            services.GetRequiredService<RoleManager<ApplicationRole>>();

        string[] roles = ["Customer", "Admin"];

        foreach (var roleName in roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var result = await roleManager.CreateAsync(
                new ApplicationRole { Name = roleName });

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ", result.Errors.Select(e => e.Description));

                throw new InvalidOperationException(
                    $"Could not create role '{roleName}': {errors}");
            }
        }
    }
}