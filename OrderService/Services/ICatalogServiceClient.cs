using OrderService.DTOs;

namespace OrderService.Services
{

    public interface ICatalogServiceClient
    {
        Task<ProductSnapshot?> GetProductAsync(
            Guid productId,
            CancellationToken cancellationToken);
    }
}
