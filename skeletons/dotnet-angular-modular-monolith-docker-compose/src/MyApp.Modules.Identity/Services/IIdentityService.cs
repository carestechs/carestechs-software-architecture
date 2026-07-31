using MyApp.Modules.Identity.DTOs;

namespace MyApp.Modules.Identity.Services;

/// <summary>Module-internal service surface (adrs/dotnet/service-layer-logic.md).</summary>
public interface IIdentityService
{
    Task<Guid> CreateUserAsync(string email, string password, string role, CancellationToken cancellationToken);
    Task<AuthenticatedUser> AuthenticateAsync(string email, string password, CancellationToken cancellationToken);
    Task<string> IssueRefreshTokenAsync(Guid userId, CancellationToken cancellationToken);
    Task<(AuthenticatedUser User, string NewToken)> RotateRefreshTokenAsync(string rawToken, CancellationToken cancellationToken);
}
