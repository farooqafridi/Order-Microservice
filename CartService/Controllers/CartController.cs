using CartService.Data;
using CartService.Models;
using CartService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly.CircuitBreaker;
using System.Security.Claims;

namespace CartService.Controllers;

[ApiController]
[Route("api/cart")]
public sealed class CartController : ControllerBase
{
    private readonly CartDbContext _dbContext;
    private readonly ICatalogServiceClient _catalogClient;

    public CartController(
        CartDbContext dbContext,
        ICatalogServiceClient catalogClient)
    {
        _dbContext = dbContext;
        _catalogClient = catalogClient;
    }

    // GET: api/cart/{customerId}
    [HttpGet]
    public async Task<IActionResult> GetCart(
        CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();

        var cart = await _dbContext.Carts
            .AsNoTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(
                c => c.CustomerId == customerId,
                cancellationToken);

        if (cart is null)
            return Ok(new
            {
                customerId,
                items = Array.Empty<object>(),
                totalAmount = 0m
            });

        return Ok(ToCartResponse(cart));
    }

    private string? GetCustomerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    // POST: api/cart/{customerId}/items
    [RequestSizeLimit(1_000_000)]

    [HttpPost("{customerId:guid}/items")]
    public async Task<IActionResult> AddItem(
        AddCartItemRequest request,
        CancellationToken cancellationToken)
    {

        var customerId = GetCustomerId();

        if (string.IsNullOrWhiteSpace(customerId))
        {
            return Unauthorized();
        }

        ProductSnapshot? product;

        try
        {
            product = await _catalogClient.GetProductAsync(
                request.ProductId, cancellationToken);
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
        catch (BrokenCircuitException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    error = "CatalogService is temporarily unavailable."
                });
        }

        if (product is null)
        {
            return BadRequest(new
            {
                message = "Product does not exist or is inactive."
            });
        }

        var cart = await _dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(
                c => c.CustomerId == customerId,
                cancellationToken);

        if (cart is null)
        {
            cart = new Cart { CustomerId = customerId };
            _dbContext.Carts.Add(cart);
        }

        var item = cart.Items.FirstOrDefault(
            i => i.ProductId == product.Id);

        if (item is not null)
        {
            if (item.Quantity > 1000 - request.Quantity)
            {
                return BadRequest(new
                {
                    message = "Maximum quantity per product is 1000."
                });
            }

            item.Quantity += request.Quantity;
            item.ProductName = product.Name;
            item.UnitPrice = product.Price;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = request.Quantity
            });
        }

        cart.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToCartResponse(cart));
    }

    // PUT: api/cart/{customerId}/items/{productId}
    [HttpPut("{customerId:guid}/items/{productId:guid}")]
    public async Task<IActionResult> UpdateItem(
        Guid productId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        var cart = await _dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(
                c => c.CustomerId == customerId,
                cancellationToken);

        if (cart is null)
            return NotFound(new { message = "Cart not found." });

        var item = cart.Items.FirstOrDefault(
            i => i.ProductId == productId);

        if (item is null)
            return NotFound(new { message = "Cart item not found." });

        ProductSnapshot? product;

        try
        {
            product = await _catalogClient.GetProductAsync(
                productId, cancellationToken);
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

        if (product is null)
        {
            return BadRequest(new
            {
                message = "Product does not exist or is inactive."
            });
        }

        item.Quantity = request.Quantity;
        item.ProductName = product.Name;
        item.UnitPrice = product.Price;
        cart.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToCartResponse(cart));
    }

    // DELETE: api/cart/{customerId}/items/{productId}
    [HttpDelete("{customerId:guid}/items/{productId:guid}")]
    public async Task<IActionResult> RemoveItem(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();

        var cart = await _dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(
                c => c.CustomerId == customerId,
                cancellationToken);

        if (cart is null)
            return NotFound(new { message = "Cart not found." });

        var item = cart.Items.FirstOrDefault(
            i => i.ProductId == productId);

        if (item is null)
            return NotFound(new { message = "Cart item not found." });

        _dbContext.CartItems.Remove(item);
        cart.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // DELETE: api/cart/{customerId}
    [HttpDelete("{customerId:guid}")]
    public async Task<IActionResult> ClearCart(
        CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();

        var cart = await _dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(
                c => c.CustomerId == customerId,
                cancellationToken);

        if (cart is null)
            return NoContent();

        _dbContext.CartItems.RemoveRange(cart.Items);
        cart.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static object ToCartResponse(Cart cart)
    {
        return new
        {
            cart.Id,
            cart.CustomerId,
            cart.CreatedAtUtc,
            cart.UpdatedAtUtc,
            Items = cart.Items.Select(item => new
            {
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity,
                item.LineTotal
            }),
            cart.TotalAmount
        };
    }
}