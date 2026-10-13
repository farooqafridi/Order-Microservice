using OrderService.Events;

namespace OrderService.Services
{
    public interface IOrderEventPublisher
    {
        Task PublishAsync(
            OrderCreated orderCreated,
            CancellationToken cancellationToken = default);
    }
}
