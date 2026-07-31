using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MyApp.Modules.Orders.Tests;

public class OrdersApiTests(OrdersApiFixture fixture) : IClassFixture<OrdersApiFixture>
{
    private const string Problem = "application/problem+json";

    [Fact]
    public async Task CreateOrder_ResolvesProductThroughTheContract()
    {
        var admin = await ClientFor("admin@example.com", "Admin123!");
        var agent = await ClientFor("agent@example.com", "Agent123!");
        var productId = await CreateProduct(admin, "SKU-ORD-1");

        var created = await agent.PostAsJsonAsync(
            "/api/orders", new { productId, quantity = 2 }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdBody = await ReadJson(created);
        var data = createdBody.RootElement.GetProperty("data");
        Assert.Equal(productId, data.GetProperty("productId").GetGuid());
        // resolved via MyApp.Contracts.ICatalogService, not a cross-module join
        Assert.Equal("Widget", data.GetProperty("productName").GetString());
        // stamped from validated claims, never from the request body
        Assert.Equal(fixture.AgentId, data.GetProperty("createdBy").GetGuid());
    }

    [Fact]
    public async Task Orders_RequireAuthentication()
    {
        var anonymous = fixture.CreateClient();
        var response = await anonymous.PostAsJsonAsync(
            "/api/orders", new { productId = Guid.NewGuid(), quantity = 1 },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.StartsWith(Problem, response.Content.Headers.ContentType!.ToString());
    }

    [Fact]
    public async Task OrderOwnership_IsEnforcedInTheService()
    {
        var admin = await ClientFor("admin@example.com", "Admin123!");
        var agent = await ClientFor("agent@example.com", "Agent123!");
        var other = await ClientFor("agent2@example.com", "Agent123!");
        var productId = await CreateProduct(admin, "SKU-ORD-2");

        var created = await agent.PostAsJsonAsync(
            "/api/orders", new { productId, quantity = 1 }, TestContext.Current.CancellationToken);
        using var createdBody = await ReadJson(created);
        var orderId = createdBody.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        // another agent gets a 404 — same as "does not exist", so IDs leak nothing
        var foreign = await other.GetAsync($"/api/orders/{orderId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);

        // the owner and an admin both succeed
        var owner = await agent.GetAsync($"/api/orders/{orderId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, owner.StatusCode);
        var elevated = await admin.GetAsync($"/api/orders/{orderId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, elevated.StatusCode);
    }

    [Fact]
    public async Task OrderForUnknownProduct_IsANotFoundProblem()
    {
        var agent = await ClientFor("agent@example.com", "Agent123!");
        var response = await agent.PostAsJsonAsync(
            "/api/orders", new { productId = Guid.NewGuid(), quantity = 1 },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.StartsWith(Problem, response.Content.Headers.ContentType!.ToString());
        using var body = await ReadJson(response);
        Assert.Equal("Not Found", body.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task InvalidQuantity_IsAValidationProblem()
    {
        var admin = await ClientFor("admin@example.com", "Admin123!");
        var agent = await ClientFor("agent@example.com", "Agent123!");
        var productId = await CreateProduct(admin, "SKU-ORD-3");
        var response = await agent.PostAsJsonAsync(
            "/api/orders", new { productId, quantity = 0 }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.StartsWith(Problem, response.Content.Headers.ContentType!.ToString());
        using var body = await ReadJson(response);
        Assert.Single(body.RootElement.GetProperty("errors").EnumerateObject());
    }

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

    private static async Task<Guid> CreateProduct(HttpClient admin, string sku)
    {
        var response = await admin.PostAsJsonAsync(
            "/api/products", new { sku, name = "Widget" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await ReadJson(response);
        return body.RootElement.GetProperty("data").GetProperty("id").GetGuid();
    }

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
}
