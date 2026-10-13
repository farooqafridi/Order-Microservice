using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Events;
using PaymentService.Models;
using System.Text.Json;

namespace PaymentService;

public sealed class Worker : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly ServiceBusSender _resultSender;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;

    public Worker(
        ServiceBusClient client,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var ordersQueue = configuration["ServiceBus:OrdersQueue"]
            ?? throw new InvalidOperationException(
                "ServiceBus:OrdersQueue is missing.");

        var resultsQueue =
            configuration["ServiceBus:PaymentResultsQueue"]
            ?? throw new InvalidOperationException(
                "ServiceBus:PaymentResultsQueue is missing.");

        _processor = client.CreateProcessor(
            ordersQueue,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            });

        _resultSender = client.CreateSender(resultsQueue);

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    private async Task ProcessMessageAsync(
        ProcessMessageEventArgs args)
    {
        OrderCreated? order;

        try
        {
            order = JsonSerializer.Deserialize<OrderCreated>(
                args.Message.Body.ToString());
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid OrderCreated message.");

            await args.DeadLetterMessageAsync(
                args.Message,
                "InvalidMessage",
                "Invalid OrderCreated JSON.",
                args.CancellationToken);

            return;
        }

        if (order is null ||
            order.OrderId == Guid.Empty ||
            order.TotalAmount <= 0)
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                "InvalidOrder",
                "Order ID or amount is invalid.",
                args.CancellationToken);

            return;
        }

        try
        {
            PaymentResult result;

            // Use a scope because DbContext is scoped.
            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<PaymentDbContext>();

                var payment = await db.Payments
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x => x.OrderId == order.OrderId,
                        args.CancellationToken);

                if (payment is null)
                {
                    _logger.LogInformation(
                        "Processing payment for order {OrderId}, amount {Amount}.",
                        order.OrderId,
                        order.TotalAmount);

                    // DEMO ONLY: replace this with your payment
                    // provider's API, using OrderId as its
                    // idempotency reference.
                    await Task.Delay(
                        TimeSpan.FromSeconds(2),
                        args.CancellationToken);

                    payment = new PaymentTransaction
                    {
                        OrderId = order.OrderId,
                        PaymentId = $"PAY-{order.OrderId:N}",
                        Amount = order.TotalAmount,
                        Status = "Succeeded",
                        ProcessedAtUtc = DateTime.UtcNow
                    };

                    db.Payments.Add(payment);

                    try
                    {
                        await db.SaveChangesAsync(
                            args.CancellationToken);
                    }
                    catch (DbUpdateException)
                    {
                        // Another delivery may have inserted
                        // the payment for this order first.
                        db.ChangeTracker.Clear();

                        payment = await db.Payments
                            .AsNoTracking()
                            .SingleOrDefaultAsync(
                                x => x.OrderId == order.OrderId,
                                args.CancellationToken);

                        if (payment is null)
                            throw;
                    }
                }

                // Never silently reuse a payment for a different
                // amount than the event requests.
                if (payment.Amount != order.TotalAmount)
                {
                    throw new InvalidOperationException(
                        $"Amount mismatch for order {order.OrderId}.");
                }

                result = new PaymentResult(
                    payment.OrderId,
                    payment.PaymentId,
                    payment.Status,
                    payment.Amount,
                    payment.ProcessedAtUtc,
                    payment.FailureReason);
            }

            // Deterministic message ID helps with duplicate
            // publication when broker duplicate detection
            // is enabled on the destination queue.
            var message = new ServiceBusMessage(
                BinaryData.FromObjectAsJson(result))
            {
                MessageId = $"payment-result-{result.OrderId}",
                ContentType = "application/json"
            };

            await _resultSender.SendMessageAsync(
                message,
                args.CancellationToken);

            // Complete the original only after publishing
            // the result succeeds.
            await args.CompleteMessageAsync(
                args.Message,
                args.CancellationToken);

            _logger.LogInformation(
                "Payment result published for order {OrderId}: {Status}.",
                result.OrderId,
                result.Status);
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
                "Payment processing or result publishing failed for {OrderId}.",
                order.OrderId);

            // In production, also configure max delivery count
            // and a dead-letter queue for repeatedly failing messages.
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
        await _resultSender.DisposeAsync();
    }
}