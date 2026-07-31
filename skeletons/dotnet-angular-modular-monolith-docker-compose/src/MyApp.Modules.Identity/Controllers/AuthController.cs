using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using MyApp.Contracts;
using MyApp.Modules.Identity.DTOs;
using MyApp.Modules.Identity.Services;

namespace MyApp.Modules.Identity.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IIdentityService identityService,
    TokenService tokenService,
    IHostEnvironment environment) : ControllerBase
{
    private const string RefreshCookie = "refresh_token";

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Login(
        LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await identityService.AuthenticateAsync(
            request.Email, request.Password, cancellationToken);
        var refresh = await identityService.IssueRefreshTokenAsync(user.Id, cancellationToken);
        SetRefreshCookie(refresh);
        return Ok(new ApiResponse<TokenResponse>(tokenService.CreateFor(user.Id, user.Role)));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Refresh(
        CancellationToken cancellationToken)
    {
        // CSRF guard for the cookie-authenticated endpoint: SameSite=Strict plus a
        // required custom header no cross-site form can set (adrs/api/jwt-bearer-auth.md)
        if (Request.Headers.XRequestedWith != "XMLHttpRequest")
        {
            throw new ForbiddenException("Missing the X-Requested-With header.");
        }

        var raw = Request.Cookies[RefreshCookie]
            ?? throw new UnauthorizedException("The refresh token is invalid or expired.");
        var (user, newRaw) = await identityService.RotateRefreshTokenAsync(raw, cancellationToken);
        SetRefreshCookie(newRaw);
        return Ok(new ApiResponse<TokenResponse>(tokenService.CreateFor(user.Id, user.Role)));
    }

    private void SetRefreshCookie(string raw)
    {
        // httpOnly + SameSite=Strict + path-scoped to the auth endpoints; Secure
        // outside local development (adrs/api/jwt-bearer-auth.md)
        Response.Cookies.Append(RefreshCookie, raw, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            MaxAge = IdentityService.RefreshTokenLifetime,
        });
    }
}
