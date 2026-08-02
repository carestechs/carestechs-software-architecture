using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MyApp.Web.Infrastructure;

namespace MyApp.Web.Features.Identity;

/// <summary>Issues 15-minute HS256 access tokens (adrs/api/jwt-bearer-auth.md).</summary>
public sealed class TokenService(IOptions<JwtOptions> jwtOptions)
{
    private readonly JsonWebTokenHandler _handler = new();

    public TokenResponse CreateFor(Guid userId, string role)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = JwtOptions.Issuer,
            Audience = JwtOptions.Audience,
            Expires = DateTime.UtcNow.Add(JwtOptions.AccessTokenLifetime),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = userId.ToString(),
                ["role"] = role,
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.Secret)),
                SecurityAlgorithms.HmacSha256),
        };
        return new TokenResponse(
            _handler.CreateToken(descriptor),
            "Bearer",
            (int)JwtOptions.AccessTokenLifetime.TotalSeconds);
    }
}
