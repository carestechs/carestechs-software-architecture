using System.ComponentModel.DataAnnotations;

namespace MyApp.Web.Features.Identity;

public sealed class LoginRequest
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}

public sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);

public sealed record AuthenticatedUser(Guid Id, string Role);
