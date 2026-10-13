namespace CartService.Models;

public sealed class Cart
{
    public Guid Id { get; set; } = Guid.NewGuid();

    //public Guid CustomerId { get; set; }

    public string CustomerId { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<CartItem> Items { get; set; } = [];

    public decimal TotalAmount =>
        Items.Sum(item => item.UnitPrice * item.Quantity);
}