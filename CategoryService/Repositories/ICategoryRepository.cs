using CategoryService.Models;

namespace CategoryService.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyCollection<Category>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Category?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task<Category> AddAsync(
        Category category,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        Category category,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}