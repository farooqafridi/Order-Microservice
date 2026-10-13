using Grpc.Core;
using InventoryService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OrderService.DTOs;
using OrderService.Events;
using OrderService.Models;
using OrderService.Repositories;
using OrderService.Services;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace OrderService.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderRepository _repository;
    private readonly Inventory.InventoryClient _inventoryClient;
    private readonly IOrderEventPublisher _eventPublisher;
    private readonly ICartServiceClient _cartClient;
    private readonly ICatalogServiceClient _catalogClient;

    public OrdersController(
        IOrderRepository repository,
        Inventory.InventoryClient inventoryClient,
        IOrderEventPublisher eventPublisher,
        ICartServiceClient cartClient,
        ICatalogServiceClient catalogClient)
    {
        _repository = repository;
        _inventoryClient = inventoryClient;
        _eventPublisher = eventPublisher;
        _cartClient = cartClient;
        _catalogClient = catalogClient;
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<Order>> Checkout(
        [FromBody] CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(customerId))
            return Unauthorized();

        var idempotencyKey = request.IdempotencyKey.Trim();

        var existing = await _repository.GetByIdempotencyKeyAsync(
            customerId, idempotencyKey, cancellationToken);

        if (existing is not null)
            return Ok(existing);

        if (!AuthenticationHeaderValue.TryParse(
                Request.Headers.Authorization.ToString(),
                out var authHeader) ||
            !string.Equals(
                authHeader.Scheme, "Bearer",
                StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(authHeader.Parameter))
        {
            return Unauthorized();
        }

        CartResponse? cart;

        try
        {
            cart = await _cartClient.GetCartAsync(
                authHeader.Parameter, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, new
            {
                message = "CartService is unavailable."
            });
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(503, new
            {
                message = "CartService request timed out."
            });
        }

        if (cart is null ||
            !string.Equals(
                cart.CustomerId, customerId,
                StringComparison.Ordinal))
        {
            return BadRequest(new
            {
                message = "Unable to retrieve the authenticated customer's cart."
            });
        }

        if (cart.Items.Count == 0)
        {
            return BadRequest(new
            {
                message = "Cannot checkout an empty cart."
            });
        }

        var order = new Order
        {
            CustomerId = customerId,
            CustomerName = User.Identity?.Name ?? customerId,
            IdempotencyKey = idempotencyKey,
            Status = "PendingPayment"
        };

        try
        {
            // Build the order using authoritative catalog data.
            foreach (var item in cart.Items)
            {
                if (item.ProductId == Guid.Empty ||
                    item.Quantity is < 1 or > 10000)
                {
                    return BadRequest(new
                    {
                        message = "The cart contains an invalid item."
                    });
                }

                var product = await _catalogClient.GetProductAsync(
                    item.ProductId, cancellationToken);

                if (product is null || !product.IsActive)
                {
                    return BadRequest(new
                    {
                        message = $"Product {item.ProductId} is unavailable."
                    });
                }

                if (product.Price <= 0)
                {
                    return BadRequest(new
                    {
                        message = $"Product {product.Id} has an invalid price."
                    });
                }

                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                });
            }
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, new
            {
                message = "CatalogService is unavailable."
            });
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(503, new
            {
                message = "CatalogService request timed out."
            });
        }

        var reservedItems = new List<(string ProductName, int Quantity)>();

        try
        {
            // Reserve stock before creating the order.
            foreach (var item in order.Items)
            {
                var result = await _inventoryClient.ReserveStockAsync(
                    new ReserveStockRequest
                    {
                        ProductName = item.ProductName,
                        Quantity = item.Quantity
                    },
                    cancellationToken: cancellationToken);

                if (!result.Reserved)
                {
                    await ReleaseReservationsAsync(reservedItems);

                    return BadRequest(new
                    {
                        message = result.Message,
                        productName = item.ProductName,
                        requestedQuantity = item.Quantity,
                        availableQuantity = result.RemainingQuantity
                    });
                }

                reservedItems.Add((item.ProductName, item.Quantity));
            }
        }
        catch (RpcException)
        {
            await ReleaseReservationsAsync(reservedItems);

            return StatusCode(503, new
            {
                message = "InventoryService is unavailable."
            });
        }
        catch (OperationCanceledException)
        {
            await ReleaseReservationsAsync(reservedItems);
            throw;
        }

        try
        {
            await _repository.AddAsync(order, cancellationToken);
        }
        catch (DbUpdateException ex) when (DatabaseExceptionHelper.IsIdempotencyUniqueConstraintViolation(ex))
        {
            var existingOrder =
                await _repository.GetByIdempotencyKeyAsync(
                    customerId, request.IdempotencyKey, cancellationToken);

            if (existingOrder is not null)
                return Ok(existingOrder);

            throw;
        }
        catch
        {
            await ReleaseReservationsAsync(reservedItems);
            throw;
        }

        // Preserve the existing event contract.
        // Do not release stock merely because publishing fails after
        // the order has been saved: the order now exists.
        var orderCreated = new OrderCreated(
            order.Id,
            order.CustomerName,
            order.TotalAmount,
            order.CreatedAtUtc);

        await _eventPublisher.PublishAsync(
            orderCreated, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = order.Id },
            order);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Order>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var customerId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(customerId))
            return Unauthorized();

        var order = await _repository.GetByIdAsync(
            id, cancellationToken);

        if (order is null || order.CustomerId != customerId)
            return NotFound();

        return Ok(order);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<Order>>> GetAll(
        CancellationToken cancellationToken)
    {
        var customerId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(customerId))
            return Unauthorized();

        var orders = await _repository.GetAllAsync(cancellationToken);

        return Ok(orders.Where(x => x.CustomerId == customerId).ToArray());
    }

    private async Task ReleaseReservationsAsync(
        List<(string ProductName, int Quantity)> reservedItems)
    {
        for (var i = reservedItems.Count - 1; i >= 0; i--)
        {
            var item = reservedItems[i];

            try
            {
                await _inventoryClient.ReleaseStockAsync(
                    new ReleaseStockRequest
                    {
                        ProductName = item.ProductName,
                        Quantity = item.Quantity
                    },
                    cancellationToken: CancellationToken.None);
            }
            catch (RpcException)
            {
                // Log this failure and schedule durable reconciliation
                // in production.
            }
        }

        reservedItems.Clear();
    }
}