using System.ComponentModel.DataAnnotations;

namespace CategoryService.Models
{
    public sealed class CreateCategoryRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }
}
