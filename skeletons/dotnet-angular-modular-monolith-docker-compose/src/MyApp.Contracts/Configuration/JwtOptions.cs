using System.ComponentModel.DataAnnotations;

namespace MyApp.Contracts.Configuration;

/// <summary>Signing configuration shared by token issuance (identity module) and
/// validation (host) — adrs/api/jwt-bearer-auth.md.</summary>
public sealed class JwtOptions
{
    public const string Issuer = "skeleton-api";
    public const string Audience = "skeleton-clients";

    /// <summary>15 minutes; the ADR caps access-token lifetime at 60.</summary>
    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);

    /// <summary>Local development only; production injects JWT_SECRET
    /// (adrs/deployment/env-connection-urls.md).</summary>
    public const string DevelopmentDefault = "dev-only-secret-change-me-minimum-32-bytes!";

    [Required]
    [MinLength(32)] // HS256 minimum key size
    public string Secret { get; set; } = string.Empty;
}
