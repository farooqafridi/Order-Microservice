using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs
{
    public sealed class CheckoutRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}
