namespace MyApp.Modules.Identity.DTOs;

public sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);

/// <summary>Authenticated principal returned by the service — never the entity
/// (adrs/dotnet/dto-at-boundary.md).</summary>
public sealed record AuthenticatedUser(Guid Id, string Role);
