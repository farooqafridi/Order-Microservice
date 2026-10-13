using System.ComponentModel.DataAnnotations;

namespace CatalogService.Models;

public sealed class UpdateProductRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal Price { get; set; }

    public Guid CategoryId { get; set; }

    [Url]
    [StringLength(2048)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }
}