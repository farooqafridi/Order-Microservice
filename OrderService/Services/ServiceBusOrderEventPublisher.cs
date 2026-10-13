using Azure.Messaging.ServiceBus;
using OrderService.Events;
using System.Text.Json;

namespace OrderService.Services
{
    public sealed class ServiceBusOrderEventPublisher
     : IOrderEventPublisher
    {
        private readonly ServiceBusSender _sender;

        public ServiceBusOrderEventPublisher(ServiceBusSender sender)
        {
            _sender = sender;
        }

        public async Task PublishAsync(
            OrderCreated orderCreated,
            CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(orderCreated);

            var message = new ServiceBusMessage(json)
            {
                MessageId = orderCreated.OrderId.ToString(),
                Subject = "OrderCreated",
                ContentType = "application/json"
            };

            await _sender.SendMessageAsync(
                message, cancellationToken);
        }
    }
}
