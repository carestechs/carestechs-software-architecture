using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MyApp.Modules.Catalog.Tests;

public class CatalogApiTests(CatalogApiFixture fixture) : IClassFixture<CatalogApiFixture>
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
        using var body = await ReadJson(duplicate);
        Assert.Equal("Conflict", body.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task MissingProduct_IsANotFoundProblem()
    {
        var client = fixture.CreateClient(); // reads are public — no token
        var response = await client.GetAsync(
            $"/api/products/{Guid.NewGuid()}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.StartsWith(Problem, response.Content.Headers.ContentType!.ToString());
    }

    [Fact]
    public async Task ValidationError_IsAProblemWithFieldDetails()
    {
        var client = await AdminClient();
        var response = await client.PostAsJsonAsync(
            "/api/products", new { sku = "", name = "" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.StartsWith(Problem, response.Content.Headers.ContentType!.ToString());
        using var body = await ReadJson(response);
        var errors = body.RootElement.GetProperty("errors");
        Assert.Equal(2, errors.EnumerateObject().Count());
    }

    private async Task<HttpClient> AdminClient()
    {
        var client = fixture.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "admin@example.com", password = "Admin123!" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var body = await ReadJson(login);
        client.DefaultRequestHeaders.Authorization = new(
            "Bearer", body.RootElement.GetProperty("data").GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
}
