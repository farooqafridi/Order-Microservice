namespace OrderService.Models;

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string CustomerId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;

    public List<OrderItem> Items { get; set; } = [];

    public decimal TotalAmount =>
        Items.Sum(item => item.Quantity * item.UnitPrice);

    public string Status { get; set; } = "PendingPayment";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

