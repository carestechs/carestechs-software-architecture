using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MyApp.Web.Tests;

public class ApiTests(AppFixture fixture) : IClassFixture<AppFixture>
{
    private const string Problem = "application/problem+json";

    [Fact]
    public async Task CreateAndList_ReturnsEnvelopeWithCamelCaseJson()
    {
        var client = await AdminClient();
        var created = await client.PostAsJsonAsync(
            "/api/products", new { sku = "SKU-1", name = "Widget" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdBody = await ReadJson(created);
        Assert.Equal("SKU-1", createdBody.RootElement.GetProperty("data").GetProperty("sku").GetString());
        Assert.True(createdBody.RootElement.GetProperty("data").TryGetProperty("createdAt", out _));

        var listed = await client.GetAsync("/api/products", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        using var listedBody = await ReadJson(listed);
        Assert.True(listedBody.RootElement.GetProperty("meta").GetProperty("totalCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task DuplicateSku_IsAConflictProblem()
    {
        var client = await AdminClient();
        var payload = new { sku = "SKU-DUP", name = "First" };
        await client.PostAsJsonAsync("/api/products", payload, TestContext.Current.CancellationToken);

        var duplicate = await client.PostAsJsonAsync(
            "/api/products", new { sku = "SKU-DUP", name = "Second" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.StartsWith(Problem, duplicate.Content.Headers.ContentType!.ToString());
    }

    [Fact]
    public async Task ProductWrite_RequiresTheAdminRole()
    {
        var anonymous = fixture.CreateClient();
        var payload = new { sku = "SKU-AUTH", name = "Widget" };

        var unauthorized = await anonymous.PostAsJsonAsync(
            "/api/products", payload, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.StartsWith(Problem, unauthorized.Content.Headers.ContentType!.ToString());
        Assert.Equal("Bearer", unauthorized.Headers.WwwAuthenticate.ToString());

        var agent = await ClientFor("agent@example.com", "Agent123!");
        var forbidden = await agent.PostAsJsonAsync(
            "/api/products", payload, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var admin = await AdminClient();
        var created = await admin.PostAsJsonAsync(
            "/api/products", payload, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    }

    [Fact]
    public async Task BackgroundJob_StampsSearchIndexedAt()
    {
        // proves the bounded channel + hosted JobRunner end to end: the fixture's
        // host runs the real BackgroundService (adrs/dotnet/in-process-background-jobs.md)
        var client = await AdminClient();
        var created = await client.PostAsJsonAsync(
            "/api/products", new { sku = "SKU-JOB", name = "Indexed" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdBody = await ReadJson(created);
        var id = createdBody.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        Assert.Equal(JsonValueKind.Null,
            createdBody.RootElement.GetProperty("data").GetProperty("searchIndexedAt").ValueKind);

        JsonElement? indexed = null;
        for (var attempt = 0; attempt < 50 && indexed is null; attempt++)
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
            var fetched = await client.GetAsync($"/api/products/{id}", TestContext.Current.CancellationToken);
            using var body = await ReadJson(fetched);
            var stamp = body.RootElement.GetProperty("data").GetProperty("searchIndexedAt");
            if (stamp.ValueKind != JsonValueKind.Null)
            {
                indexed = stamp.Clone();
            }
        }

        Assert.NotNull(indexed); // the in-process job ran and stamped the row
    }

    [Fact]
    public async Task RefreshRotation_ReuseRevokesTheFamily()
    {
        var client = fixture.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "agent@example.com", password = "Agent123!" },
            TestContext.Current.CancellationToken);
        var first = ExtractRefreshToken(login);

        var rotated = await Refresh(client, first);
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        var second = ExtractRefreshToken(rotated);

        var reuse = await Refresh(client, first);
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        var afterRevoke = await Refresh(client, second);
        Assert.Equal(HttpStatusCode.Unauthorized, afterRevoke.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshFamily()
    {
        var client = fixture.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "agent@example.com", password = "Agent123!" },
            TestContext.Current.CancellationToken);
        var refreshToken = ExtractRefreshToken(login);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Add("Cookie", $"refresh_token={refreshToken}");
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var reuse = await Refresh(client, refreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task SpaFallback_ServesHtmlButNeverForApiRoutes()
    {
        // adrs/deployment/spa-served-by-api.md: unknown app routes fall back to
        // index.html; unknown API routes stay Problem Details
        var client = fixture.CreateClient();

        var spa = await client.GetAsync("/some/spa/route", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, spa.StatusCode);
        Assert.StartsWith("text/html", spa.Content.Headers.ContentType!.ToString());

        var api = await client.GetAsync("/api/nope", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, api.StatusCode);
        Assert.StartsWith(Problem, api.Content.Headers.ContentType!.ToString());
    }

    private async Task<HttpClient> AdminClient() => await ClientFor("admin@example.com", "Admin123!");

    private async Task<HttpClient> ClientFor(string email, string password)
    {
        var client = fixture.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/auth/login", new { email, password }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var body = await ReadJson(login);
        client.DefaultRequestHeaders.Authorization = new(
            "Bearer", body.RootElement.GetProperty("data").GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<HttpResponseMessage> Refresh(HttpClient client, string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refresh_token={refreshToken}");
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
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
