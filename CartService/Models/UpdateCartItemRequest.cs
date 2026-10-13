using System.ComponentModel.DataAnnotations;

namespace CartService.Models;

public sealed class UpdateCartItemRequest
{
    [Range(1, 1000)]
    public int Quantity { get; set; }
}