namespace OrderService.Models
{
    public sealed record PaymentResult(
    Guid OrderId,
    string PaymentId,
    string Status,
    decimal Amount,
    DateTime ProcessedAtUtc,
    string? FailureReason);
}
