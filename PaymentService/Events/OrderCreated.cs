namespace PaymentService.Events
{
    public sealed record OrderCreated(
    Guid OrderId,
    string CustomerName,
    decimal TotalAmount,
    DateTime CreatedAtUtc);
}
