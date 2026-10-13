using CategoryService.Models;
using CategoryService.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CategoryService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _repository;

    public CategoriesController(ICategoryRepository repository)
    {
        _repository = repository;
    }

    // GET: api/categories
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<Category>>> GetAll(
        CancellationToken cancellationToken)
    {
        var categories = await _repository.GetAllAsync(cancellationToken);

        return Ok(categories.Where(c => c.IsActive).ToList());
    }

    // GET: api/categories/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Category>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(
            id, cancellationToken);

        if (category is null || !category.IsActive)
            return NotFound();

        return Ok(category);
    }

    // POST: api/categories
    [HttpPost]
    public async Task<ActionResult<Category>> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        if (await _repository.GetByNameAsync(name, cancellationToken)
            is not null)
        {
            return Conflict(new
            {
                message = "A category with this name already exists."
            });
        }

        var category = new Category
        {
            Name = name,
            Description = request.Description.Trim()
        };

        try
        {
            await _repository.AddAsync(category, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The unique database index also protects against
            // concurrent requests creating the same name.
            return Conflict(new
            {
                message = "The category could not be created. " +
                          "Check whether its name already exists."
            });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = category.Id },
            category);
    }

    // PUT: api/categories/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(
            id, cancellationToken);

        if (existing is null)
            return NotFound();

        var name = request.Name.Trim();

        var duplicate = await _repository.GetByNameAsync(
            name, cancellationToken);

        if (duplicate is not null && duplicate.Id != id)
        {
            return Conflict(new
            {
                message = "A category with this name already exists."
            });
        }

        existing.Name = name;
        existing.Description = request.Description.Trim();
        existing.IsActive = request.IsActive;

        try
        {
            var updated = await _repository.UpdateAsync(
                existing, cancellationToken);

            return updated ? NoContent() : NotFound();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "The category could not be updated. " +
                          "Check whether its name already exists."
            });
        }
    }

    // DELETE: api/categories/{id}
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