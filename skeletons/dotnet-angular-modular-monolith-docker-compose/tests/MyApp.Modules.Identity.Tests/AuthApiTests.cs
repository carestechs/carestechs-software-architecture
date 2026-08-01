using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MyApp.Modules.Identity.Tests;

public class AuthApiTests(IdentityApiFixture fixture) : IClassFixture<IdentityApiFixture>
{
    private const string Problem = "application/problem+json";

    // Cookies are asserted and replayed manually, so the client must not manage them.
    private HttpClient CreateRawClient() =>
        fixture.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    [Fact]
    public async Task Login_ReturnsTokenAndRefreshCookie()
    {
        var client = CreateRawClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "agent@example.com", password = "Agent123!" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = await ReadJson(response);
        var data = body.RootElement.GetProperty("data");
        Assert.Equal("Bearer", data.GetProperty("tokenType").GetString());
        Assert.Equal(900, data.GetProperty("expiresIn").GetInt32()); // 15 minutes
        Assert.Equal(2, data.GetProperty("accessToken").GetString()!.Count(c => c == '.'));

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("refresh_token=", cookie);
        Assert.Contains("httponly", cookie.ToLowerInvariant());
        Assert.Contains("samesite=strict", cookie.ToLowerInvariant());
        Assert.Contains("path=/api/auth", cookie.ToLowerInvariant());
    }

    [Fact]
    public async Task WrongPassword_IsA401Problem()
    {
        var client = CreateRawClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "agent@example.com", password = "nope" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.StartsWith(Problem, response.Content.Headers.ContentType!.ToString());
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task Refresh_RotatesAndReuseRevokesTheFamily()
    {
        var client = CreateRawClient();
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "agent@example.com", password = "Agent123!" },
            TestContext.Current.CancellationToken);
        var first = ExtractRefreshToken(login);

        // CSRF guard: cookie alone is not enough
        var noHeader = await Refresh(client, first, withCsrfHeader: false);
        Assert.Equal(HttpStatusCode.Forbidden, noHeader.StatusCode);

        var rotated = await Refresh(client, first);
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        var second = ExtractRefreshToken(rotated);
        Assert.NotEqual(first, second);

        // Reusing the ALREADY-ROTATED first token must revoke the whole family
        var reuse = await Refresh(client, first);
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        // ... including the otherwise-valid second token
        var afterRevoke = await Refresh(client, second);
        Assert.Equal(HttpStatusCode.Unauthorized, afterRevoke.StatusCode);
    }

    [Fact]
    public async Task ProductWrite_RequiresTheAdminRole()
    {
        var client = CreateRawClient();
        var payload = new { sku = "SKU-AUTH", name = "Widget" };

        var anonymous = await client.PostAsJsonAsync(
            "/api/products", payload, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.StartsWith(Problem, anonymous.Content.Headers.ContentType!.ToString());
        Assert.Equal("Bearer", anonymous.Headers.WwwAuthenticate.ToString());

        var agentToken = await LoginFor(client, "agent@example.com", "Agent123!");
        using (var forbidden = ForRole(client, "/api/products", payload, agentToken))
        {
            var response = await client.SendAsync(forbidden, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.StartsWith(Problem, response.Content.Headers.ContentType!.ToString());
        }

        var adminToken = await LoginFor(client, "admin@example.com", "Admin123!");
        using (var allowed = ForRole(client, "/api/products", payload, adminToken))
        {
            var response = await client.SendAsync(allowed, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshFamily()
    {
        var client = CreateRawClient();
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "agent@example.com", password = "Agent123!" },
            TestContext.Current.CancellationToken);
        var refreshToken = ExtractRefreshToken(login);

        // CSRF guard applies to logout exactly as to refresh
        var noHeader = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        noHeader.Headers.Add("Cookie", $"refresh_token={refreshToken}");
        var forbidden = await client.SendAsync(noHeader, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Add("Cookie", $"refresh_token={refreshToken}");
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        // the cookie is cleared on the way out
        var cleared = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("refresh_token=", cleared);
        Assert.Contains("path=/api/auth", cleared.ToLowerInvariant());

        // the revoked family can no longer refresh
        var reuse = await Refresh(client, refreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutACookie_IsANoOp()
    {
        var client = CreateRawClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static HttpRequestMessage ForRole(HttpClient client, string url, object payload, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Authorization = new("Bearer", token);
        return request;
    }

    private static async Task<string> LoginFor(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new { email, password }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await ReadJson(response);
        return body.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private static async Task<HttpResponseMessage> Refresh(
        HttpClient client, string refreshToken, bool withCsrfHeader = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refresh_token={refreshToken}");
        if (withCsrfHeader)
        {
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        }
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static string ExtractRefreshToken(HttpResponseMessage response)
    {
        var cookie = response.Headers.GetValues("Set-Cookie")
            .Single(c => c.StartsWith("refresh_token="));
        return cookie.Split(';')[0]["refresh_token=".Length..];
    }

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
}
