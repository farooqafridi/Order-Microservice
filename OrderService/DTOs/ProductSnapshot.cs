namespace OrderService.DTOs
{
    public sealed class ProductSnapshot
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public bool IsActive { get; set; }
    }
}
