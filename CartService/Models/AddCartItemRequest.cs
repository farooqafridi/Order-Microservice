using System.ComponentModel.DataAnnotations;

namespace CartService.Models;

public sealed class AddCartItemRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; }
}