using CatalogService.Models;

namespace CatalogService.Repositories;

public sealed class InMemoryProductRepository
    
{
    private readonly List<Product> _products = [];
    private readonly object _lock = new();

    public IReadOnlyCollection<Product> GetAll()
    {
        lock (_lock)
        {
            return _products
                .Select(Copy)
                .ToList();
        }
    }

    public Product? GetById(Guid id)
    {
        lock (_lock)
        {
            var product = _products
                .FirstOrDefault(p => p.Id == id);

            return product is null ? null : Copy(product);
        }
    }

    public IReadOnlyCollection<Product> GetByCategory(
        Guid categoryId)
    {
        lock (_lock)
        {
            return _products
                .Where(p => p.CategoryId == categoryId)
                .Select(Copy)
                .ToList();
        }
    }

    public Product Add(Product product)
    {
        lock (_lock)
        {
            _products.Add(Copy(product));
            return Copy(product);
        }
    }

    public bool Update(Product product)
    {
        lock (_lock)
        {
            var existing = _products
                .FirstOrDefault(p => p.Id == product.Id);

            if (existing is null)
                return false;

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.Price = product.Price;
            existing.CategoryId = product.CategoryId;
            existing.ImageUrl = product.ImageUrl;
            existing.IsActive = product.IsActive;

            return true;
        }
    }

    public bool Delete(Guid id)
    {
        lock (_lock)
        {
            var product = _products
                .FirstOrDefault(p => p.Id == id);

            return product is not null &&
                   _products.Remove(product);
        }
    }

    private static Product Copy(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        CategoryId = product.CategoryId,
        ImageUrl = product.ImageUrl,
        IsActive = product.IsActive,
        CreatedAtUtc = product.CreatedAtUtc
    };
}