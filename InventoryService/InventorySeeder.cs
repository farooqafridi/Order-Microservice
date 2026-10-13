using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Data;

public static class InventorySeeder
{
    public static async Task SeedAsync(
        InventoryDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.InventoryItems.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.InventoryItems.AddRange(
            new InventoryItem
            {
                ProductId = Guid.Parse(
                    "8EE175D5-2793-4327-9AA3-06DDFEEED7CD"),
                ProductName = "Laptop",
                AvailableQuantity = 10
            },
            new InventoryItem
            {
                ProductId = Guid.Parse(
                    "93D2B2E1-E0AE-47B7-88B0-4DE7A9B8240C"),
                ProductName = "Laptop 1",
                AvailableQuantity = 50
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}