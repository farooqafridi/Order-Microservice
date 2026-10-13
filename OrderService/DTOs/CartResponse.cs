namespace OrderService.DTOs
{
    public sealed class CartResponse
    {
        public string CustomerId { get; set; } = string.Empty;

        public List<CartItemResponse> Items { get; set; } = [];

        public decimal TotalAmount { get; set; }
    }

    public sealed class CartItemResponse
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public decimal LineTotal { get; set; }
    }
}
