using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Workers;

public sealed class PaymentResultWorker : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentResultWorker> _logger;

    public PaymentResultWorker(
        ServiceBusClient client,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<PaymentResultWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var queueName =
            configuration["ServiceBus:PaymentResultsQueue"]
            ?? throw new InvalidOperationException(
                "ServiceBus:PaymentResultsQueue is missing.");

        _processor = client.CreateProcessor(
            queueName,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    private async Task ProcessMessageAsync(
        ProcessMessageEventArgs args)
    {
        PaymentResult? result;

        // 1. Deserialize the message.
        try
        {
            result = JsonSerializer.Deserialize<PaymentResult>(
                args.Message.Body.ToString());
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid PaymentResult message.");

            await args.DeadLetterMessageAsync(
                args.Message,
                "InvalidPaymentResult",
                "Message body contains invalid JSON.",
                args.CancellationToken);

            return;
        }

        // 2. Validate the payment result.
        if (result is null ||
            result.OrderId == Guid.Empty ||
            result.Amount <= 0 ||
            string.IsNullOrWhiteSpace(result.PaymentId))
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                "InvalidPaymentResult",
                "Required payment result fields are invalid.",
                args.CancellationToken);

            return;
        }

        if (result.Status is not ("Succeeded" or "Failed"))
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                "UnknownPaymentStatus",
                $"Unsupported payment status: {result.Status}",
                args.CancellationToken);

            return;
        }

        try
        {
            await using var scope =
                _scopeFactory.CreateAsyncScope();

            var db = scope.ServiceProvider
                .GetRequiredService<OrderDbContext>();

            // 3. Load the order and its items.
            var order = await db.Orders
                .Include(x => x.Items)
                .SingleOrDefaultAsync(
                    x => x.Id == result.OrderId,
                    args.CancellationToken);

            if (order is null)
            {
                // The order may not be available yet, or the
                // message may refer to an invalid order.
                // Retry, then allow Service Bus to dead-letter
                // it after the configured delivery limit.
                throw new InvalidOperationException(
                    $"Order {result.OrderId} was not found.");
            }

            // 4. Verify that the payment matches the order.
            if (order.TotalAmount != result.Amount)
            {
                await args.DeadLetterMessageAsync(
                    args.Message,
                    "PaymentAmountMismatch",
                    "Payment amount does not match order total.",
                    args.CancellationToken);

                return;
            }

            // 5. Make repeated delivery safe.
            if (order.Status == "Paid")
            {
                if (result.Status != "Succeeded")
                {
                    await args.DeadLetterMessageAsync(
                        args.Message,
                        "ConflictingPaymentResult",
                        "Order is already Paid but received a failure result.",
                        args.CancellationToken);

                    return;
                }

                _logger.LogInformation(
                    "Order {OrderId} is already Paid. Ignoring duplicate result.",
                    order.Id);

                await args.CompleteMessageAsync(
                    args.Message,
                    args.CancellationToken);

                return;
            }

            if (order.Status == "PaymentFailed")
            {
                if (result.Status != "Failed")
                {
                    await args.DeadLetterMessageAsync(
                        args.Message,
                        "ConflictingPaymentResult",
                        "Order already failed but received a success result.",
                        args.CancellationToken);

                    return;
                }

                await args.CompleteMessageAsync(
                    args.Message,
                    args.CancellationToken);

                return;
            }

            if (order.Status != "PendingPayment")
            {
                await args.DeadLetterMessageAsync(
                    args.Message,
                    "InvalidOrderState",
                    $"Cannot apply payment result to order status {order.Status}.",
                    args.CancellationToken);

                return;
            }

            // 6. Update the order status.
            order.Status = result.Status == "Succeeded"
                ? "Paid"
                : "PaymentFailed";

            await db.SaveChangesAsync(args.CancellationToken);

            _logger.LogInformation(
                "Order {OrderId} updated to {Status}. PaymentId: {PaymentId}",
                order.Id,
                order.Status,
                result.PaymentId);

            // 7. Complete only after saving succeeds.
            await args.CompleteMessageAsync(
                args.Message,
                args.CancellationToken);
        }
        catch (OperationCanceledException)
            when (args.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process payment result for order {OrderId}.",
                result.OrderId);

            // Release the message for retry.
            await args.AbandonMessageAsync(
                args.Message,
                cancellationToken: args.CancellationToken);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Service Bus error. Entity: {EntityPath}, Source: {ErrorSource}",
            args.EntityPath,
            args.ErrorSource);

        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await _processor.StartProcessingAsync(stoppingToken);

        try
        {
            await Task.Delay(
                Timeout.InfiniteTimeSpan,
                stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Expected during shutdown.
        }
        finally
        {
            await _processor.StopProcessingAsync(
                CancellationToken.None);
        }
    }

    public override async Task StopAsync(
        CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await _processor.DisposeAsync();
    }
}