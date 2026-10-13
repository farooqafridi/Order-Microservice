using System.ComponentModel.DataAnnotations;

namespace UserService.DTOs;

public sealed class RegisterRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 7)]
    public string Password { get; set; } = string.Empty;
}