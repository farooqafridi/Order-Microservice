using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using UserService.Data;

namespace UserService.IntegrationTests;

public sealed class CustomWebApplicationFactory: WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        //var connectionString =
        //    Environment.GetEnvironmentVariable(
        //        "UserDb")
        //    ?? throw new InvalidOperationException(
        //        "USER_TEST_CONNECTION_STRING is not configured.");

        var connectionString = "Data Source=DESKTOP-AE7G9LP;Initial Catalog=UserDb;Integrated Security=True;TrustServerCertificate=True;";


        builder.ConfigureServices(services =>
        {
            // Replace the application's DbContext registration.
            services.Remove(
                services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<UserDbContext>))
                ?? throw new InvalidOperationException(
                    "UserDbContext options registration was not found."));

            services.AddDbContext<UserDbContext>(options =>
                options.UseSqlServer(connectionString));
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<UserDbContext>();

        await db.Database.MigrateAsync();
    }
}