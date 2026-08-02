using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyApp.Web.Infrastructure;

namespace MyApp.Web.Features.Identity;

public interface IIdentityService
{
    Task<Guid> CreateUserAsync(string email, string password, string role, CancellationToken cancellationToken);
    Task<AuthenticatedUser> AuthenticateAsync(string email, string password, CancellationToken cancellationToken);
    Task<string> IssueRefreshTokenAsync(Guid userId, CancellationToken cancellationToken);
    Task<(AuthenticatedUser User, string NewToken)> RotateRefreshTokenAsync(string rawToken, CancellationToken cancellationToken);
    Task RevokeRefreshFamilyAsync(string rawToken, CancellationToken cancellationToken);
}

public class IdentityService(AppDbContext db, IPasswordHasher<User> passwordHasher) : IIdentityService
{
    /// <summary>Absolute maximum, never extended by rotation (adrs/api/jwt-bearer-auth.md).</summary>
    public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<Guid> CreateUserAsync(
        string email, string password, string role, CancellationToken cancellationToken)
    {
        var duplicate = await db.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (duplicate)
        {
            throw new ConflictException($"A user with email '{email}' already exists.");
        }

        var user = new User { Email = email, Role = role };
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    public async Task<AuthenticatedUser> AuthenticateAsync(
        string email, string password, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        // one generic message for unknown email and wrong password alike
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password)
                == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException("Invalid credentials.");
        }

        return new AuthenticatedUser(user.Id, user.Role);
    }

    public async Task<string> IssueRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await IssueAsync(userId, Guid.CreateVersion7(),
            DateTimeOffset.UtcNow.Add(RefreshTokenLifetime), cancellationToken);
    }

    public async Task<(AuthenticatedUser User, string NewToken)> RotateRefreshTokenAsync(
        string rawToken, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var hash = HashToken(rawToken);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(
            t => t.TokenHash == hash, cancellationToken);
        if (token is null || token.RevokedAt is not null || token.ExpiresAt <= now)
        {
            throw new UnauthorizedException("The refresh token is invalid or expired.");
        }

        if (token.UsedAt is not null)
        {
            // Reuse detected: this token was already rotated once. Revoke the family.
            await db.RefreshTokens
                .Where(t => t.FamilyId == token.FamilyId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);
            throw new UnauthorizedException("Refresh token reuse detected; please sign in again.");
        }

        token.UsedAt = now;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == token.UserId, cancellationToken)
            ?? throw new UnauthorizedException("The refresh token is invalid or expired.");

        var newRaw = await IssueAsync(user.Id, token.FamilyId, token.ExpiresAt, cancellationToken);
        return (new AuthenticatedUser(user.Id, user.Role), newRaw);
    }

    public async Task RevokeRefreshFamilyAsync(string rawToken, CancellationToken cancellationToken)
    {
        var hash = HashToken(rawToken);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(
            t => t.TokenHash == hash, cancellationToken);
        if (token is null)
        {
            return; // idempotent logout
        }

        var now = DateTimeOffset.UtcNow;
        await db.RefreshTokens
            .Where(t => t.FamilyId == token.FamilyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);
    }

    private async Task<string> IssueAsync(
        Guid userId, Guid familyId, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(raw),
            FamilyId = familyId,
            ExpiresAt = expiresAt,
        });
        await db.SaveChangesAsync(cancellationToken);
        return raw;
    }

    private static string HashToken(string raw) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
}
