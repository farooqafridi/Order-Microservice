using CatalogService.Models;
using CatalogService.Repositories;
using CatalogService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;
    private readonly ICategoryServiceClient _categoryServiceClient;

    public ProductsController(
     IProductRepository repository,
     ICategoryServiceClient categoryServiceClient)
    {
        _repository = repository;
        _categoryServiceClient = categoryServiceClient;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<Product>>> GetAll(
        CancellationToken cancellationToken)
    {
        var products = await _repository.GetAllAsync(cancellationToken);

        return Ok(products.Where(p => p.IsActive).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Product>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(
            id, cancellationToken);

        if (product is null || !product.IsActive)
            return NotFound();

        return Ok(product);
    }

    [HttpGet("category/{categoryId:guid}")]
    public async Task<ActionResult<IReadOnlyCollection<Product>>>
        GetByCategory(
            Guid categoryId,
            CancellationToken cancellationToken)
    {
        var products = await _repository.GetByCategoryAsync(
            categoryId, cancellationToken);

        return Ok(products.Where(p => p.IsActive).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CategoryId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "A valid category ID is required."
            });
        }

        try
        {
            var categoryIsActive = await _categoryServiceClient.IsActiveAsync(
                request.CategoryId,
                cancellationToken);

            if (!categoryIsActive)
            {
                return BadRequest(new
                {
                    message = "The specified category does not exist or is inactive."
                });
            }
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "CategoryService is currently unavailable."
            });
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "CategoryService request timed out."
            });
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            CategoryId = request.CategoryId,
            ImageUrl = request.ImageUrl
        };

        await _repository.AddAsync(product, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(
            id, cancellationToken);

        if (existing is null)
            return NotFound();

        existing.Name = request.Name.Trim();
        existing.Description = request.Description.Trim();
        existing.Price = request.Price;
        existing.CategoryId = request.CategoryId;
        existing.ImageUrl = request.ImageUrl;
        existing.IsActive = request.IsActive;

        var updated = await _repository.UpdateAsync(
            existing, cancellationToken);

        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _repository.DeleteAsync(
            id, cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}