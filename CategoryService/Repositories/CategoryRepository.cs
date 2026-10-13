using CategoryService.Data;
using CategoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace CategoryService.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly CategoryDbContext _dbContext;

    public CategoryRepository(CategoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Category>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Category?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == id,
                cancellationToken);
    }

    public Task<Category?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Name == name,
                cancellationToken);
    }

    public async Task<Category> AddAsync(
        Category category,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Categories.Add(category);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return category;
    }

    public async Task<bool> UpdateAsync(
        Category category,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Categories
            .FirstOrDefaultAsync(
                c => c.Id == category.Id,
                cancellationToken);

        if (existing is null)
            return false;

        existing.Name = category.Name;
        existing.Description = category.Description;
        existing.IsActive = category.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(
                c => c.Id == id,
                cancellationToken);

        if (category is null)
            return false;

        _dbContext.Categories.Remove(category);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}