namespace CartService.Models;

public sealed class CartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CartId { get; set; }

    public Cart Cart { get; set; } = null!;

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    // Price snapshot for displaying the cart, not checkout authority.
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;

    public decimal LineTotal => UnitPrice * Quantity;
}