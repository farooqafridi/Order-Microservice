using OrderService.DTOs;

namespace OrderService.Services
{
    public interface ICartServiceClient
    {
        Task<CartResponse?> GetCartAsync(
            string accessToken,
            CancellationToken cancellationToken);
    }
}
