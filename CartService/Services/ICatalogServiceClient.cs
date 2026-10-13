using CartService.Models;

namespace CartService.Services
{
    public interface ICatalogServiceClient
    {
        Task<ProductSnapshot?> GetProductAsync(
            Guid productId,
            CancellationToken cancellationToken = default);
    }
}
