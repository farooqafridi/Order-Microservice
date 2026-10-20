using CartService.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CompanyApi.IntegrationTests;

public sealed class CustomWebApplicationFactory: WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {


        var connectionString =
            Environment.GetEnvironmentVariable(
                "CartDb")
            ?? throw new InvalidOperationException(
                "Set INTEGRATION_TEST_SQLSERVER.");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<CartDbContext>();
            services.RemoveAll<
                DbContextOptions<CartDbContext>>();

            services.AddDbContext<CartDbContext>(options =>
                options.UseSqlServer(connectionString));
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<CartDbContext>();

        await db.Database.MigrateAsync();
    }
}