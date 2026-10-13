using Grpc.Core;
using InventoryService.Data;
using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Services;

public sealed class InventoryGrpcService : Inventory.InventoryBase
{
    private readonly InventoryDbContext _dbContext;

    public InventoryGrpcService(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override async Task<CheckStockReply> CheckStock(
        CheckStockRequest request,
        ServerCallContext context)
    {
        ValidateRequest(request.ProductName, request.Quantity);

        var item = await FindItemAsync(
            request.ProductName,
            context.CancellationToken);

        if (item is null)
        {
            return new CheckStockReply
            {
                Available = false,
                AvailableQuantity = 0,
                Message = "Product not found."
            };
        }

        var available = item.AvailableQuantity >= request.Quantity;

        return new CheckStockReply
        {
            Available = available,
            AvailableQuantity = item.AvailableQuantity,
            Message = available
                ? "Stock is available."
                : "Insufficient stock."
        };
    }

    public override async Task<ReserveStockReply> ReserveStock(
        ReserveStockRequest request,
        ServerCallContext context)
    {
        ValidateRequest(request.ProductName, request.Quantity);

        var productName = request.ProductName.Trim();

        // A single SQL UPDATE checks stock and decrements it atomically.
        var updated = await _dbContext.InventoryItems
            .Where(x =>
                x.ProductName == productName &&
                x.AvailableQuantity >= request.Quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        x => x.AvailableQuantity,
                        x => x.AvailableQuantity - request.Quantity)
                    .SetProperty(
                        x => x.UpdatedAtUtc,
                        DateTime.UtcNow),
                context.CancellationToken);

        if (updated == 1)
        {
            var remaining = await GetAvailableQuantityAsync(
                productName,
                context.CancellationToken);

            return new ReserveStockReply
            {
                Reserved = true,
                RemainingQuantity = remaining ?? 0,
                Message = "Stock reserved successfully."
            };
        }

        var currentQuantity = await GetAvailableQuantityAsync(
            productName,
            context.CancellationToken);

        if (currentQuantity is null)
        {
            return new ReserveStockReply
            {
                Reserved = false,
                RemainingQuantity = 0,
                Message = "Product not found."
            };
        }

        return new ReserveStockReply
        {
            Reserved = false,
            RemainingQuantity = currentQuantity.Value,
            Message = "Insufficient stock."
        };
    }

    public override async Task<ReleaseStockReply> ReleaseStock(
        ReleaseStockRequest request,
        ServerCallContext context)
    {
        ValidateRequest(request.ProductName, request.Quantity);

        var productName = request.ProductName.Trim();

        try
        {
            var updated = await _dbContext.InventoryItems
                .Where(x => x.ProductName == productName)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            x => x.AvailableQuantity,
                            x => x.AvailableQuantity + request.Quantity)
                        .SetProperty(
                            x => x.UpdatedAtUtc,
                            DateTime.UtcNow),
                    context.CancellationToken);

            if (updated == 0)
            {
                return new ReleaseStockReply
                {
                    Released = false,
                    AvailableQuantity = 0,
                    Message = "Product not found."
                };
            }

            var quantity = await GetAvailableQuantityAsync(
                productName,
                context.CancellationToken);

            return new ReleaseStockReply
            {
                Released = true,
                AvailableQuantity = quantity ?? 0,
                Message = "Stock released successfully."
            };
        }
        catch (DbUpdateException ex)
        {
            throw new RpcException(new Status(
                StatusCode.OutOfRange,
                $"Unable to release stock: {ex.Message}"));
        }
    }

    private async Task<InventoryItem?> FindItemAsync(
        string productName,
        CancellationToken cancellationToken)
    {
        var normalizedName = productName.Trim();

        return await _dbContext.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.ProductName == normalizedName,
                cancellationToken);
    }

    private async Task<int?> GetAvailableQuantityAsync(
        string productName,
        CancellationToken cancellationToken)
    {
        return await _dbContext.InventoryItems
            .AsNoTracking()
            .Where(x => x.ProductName == productName)
            .Select(x => (int?)x.AvailableQuantity)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static void ValidateRequest(
        string productName,
        int quantity)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "Product name is required."));
        }

        if (quantity <= 0)
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "Quantity must be greater than zero."));
        }
    }
}