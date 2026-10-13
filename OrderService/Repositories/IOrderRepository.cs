using OrderService.Models;

namespace OrderService.Repositories;

public interface IOrderRepository
{
    Task<Order> AddAsync(
        Order order,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Order>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdempotencyKeyAsync(
    string customerId,
    string idempotencyKey,
    CancellationToken cancellationToken = default);
}