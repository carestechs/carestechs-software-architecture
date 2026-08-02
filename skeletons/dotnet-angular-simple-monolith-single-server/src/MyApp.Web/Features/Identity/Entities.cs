namespace MyApp.Web.Features.Identity;

public class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // 'admin' | 'agent'
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>One row per issued refresh token; rotation chains rows through FamilyId
/// so reuse of an already-rotated token revokes the whole family
/// (adrs/api/jwt-bearer-auth.md).</summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty; // sha256 hex; never the raw token
    public Guid FamilyId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; } // absolute family bound
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
