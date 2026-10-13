namespace InventoryService.Models
{
    public sealed class InventoryItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int AvailableQuantity { get; set; }

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        // EF Core uses this token to detect concurrent updates.
        public byte[] RowVersion { get; set; } = [];
    }
}
