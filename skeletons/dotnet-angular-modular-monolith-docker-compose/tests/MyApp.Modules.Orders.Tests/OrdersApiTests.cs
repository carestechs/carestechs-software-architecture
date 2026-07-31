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
        var client = fixture.CreateClient();
        var productId = await CreateProduct(client, "SKU-ORD-1");

        var created = await client.PostAsJsonAsync(
            "/api/orders", new { productId, quantity = 2 }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdBody = await ReadJson(created);
        var data = createdBody.RootElement.GetProperty("data");
        Assert.Equal(productId, data.GetProperty("productId").GetGuid());
        // resolved via MyApp.Contracts.ICatalogService, not a cross-module join
        Assert.Equal("Widget", data.GetProperty("productName").GetString());

        var fetched = await client.GetAsync(
            $"/api/orders/{data.GetProperty("id").GetGuid()}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        using var fetchedBody = await ReadJson(fetched);
        Assert.Equal("Widget", fetchedBody.RootElement.GetProperty("data").GetProperty("productName").GetString());
    }

    [Fact]
    public async Task OrderForUnknownProduct_IsANotFoundProblem()
    {
        var client = fixture.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/orders", new { productId = Guid.NewGuid(), quantity = 1 }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.StartsWith(Problem, response.Content.Headers.ContentType!.ToString());
        using var body = await ReadJson(response);
        Assert.Equal("Not Found", body.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task MissingOrder_IsANotFoundProblem()
    {
        var client = fixture.CreateClient();
        var response = await client.GetAsync(
            $"/api/orders/{Guid.NewGuid()}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.StartsWith(Problem, response.Content.Headers.ContentType!.ToString());
    }

    [Fact]
    public async Task InvalidQuantity_IsAValidationProblem()
    {
        var client = fixture.CreateClient();
        var productId = await CreateProduct(client, "SKU-ORD-2");
        var response = await client.PostAsJsonAsync(
            "/api/orders", new { productId, quantity = 0 }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.StartsWith(Problem, response.Content.Headers.ContentType!.ToString());
        using var body = await ReadJson(response);
        Assert.Equal(1, body.RootElement.GetProperty("errors").EnumerateObject().Count());
    }

    private static async Task<Guid> CreateProduct(HttpClient client, string sku)
    {
        var response = await client.PostAsJsonAsync(
            "/api/products", new { sku, name = "Widget" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await ReadJson(response);
        return body.RootElement.GetProperty("data").GetProperty("id").GetGuid();
    }

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
}
