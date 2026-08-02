using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Catalog.Tests;

/// <summary>Integration against the Flyway-migrated PostgreSQL. The schema is
/// owned by Flyway (adrs/deployment/flyway-migrations.md) — the fixture never
/// creates or drops it; tests append rows with unique SKUs. Skips cleanly when
/// no database is configured (local machines without Docker).</summary>
public sealed class CatalogApiFixture : WebApplicationFactory<Program>
{
    public static readonly string? ConnectionString =
        Environment.GetEnvironmentVariable("TEST_DATABASE_URL");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("DATABASE_URL", ConnectionString ?? "unset");
        builder.UseEnvironment("Development");
    }
}

public class CatalogApiIntegrationTests(CatalogApiFixture fixture) : IClassFixture<CatalogApiFixture>
{
    private const string Problem = "application/problem+json";

    [Fact]
    public async Task CreateGetAndConflict_AgainstTheFlywaySchema()
    {
        Assert.SkipWhen(CatalogApiFixture.ConnectionString is null,
            "TEST_DATABASE_URL not set — integration runs in CI against the Flyway-migrated database.");

        var client = fixture.CreateClient();
        var sku = $"SKU-{Guid.CreateVersion7():N}"[..24];

        var created = await client.PostAsJsonAsync(
            "/v1/products", new { sku, name = "Widget" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdBody = JsonDocument.Parse(
            await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var id = createdBody.RootElement.GetProperty("id").GetGuid();

        // bare DTO — rest-envelope is deliberately NOT part of this profile's Required set
        var fetched = await client.GetAsync($"/v1/products/{id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        using var fetchedBody = JsonDocument.Parse(
            await fetched.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(sku.ToUpperInvariant(), fetchedBody.RootElement.GetProperty("sku").GetString());

        var duplicate = await client.PostAsJsonAsync(
            "/v1/products", new { sku, name = "Other" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.StartsWith(Problem, duplicate.Content.Headers.ContentType!.ToString());

        var missing = await client.GetAsync(
            $"/v1/products/{Guid.CreateVersion7()}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.StartsWith(Problem, missing.Content.Headers.ContentType!.ToString());
    }
}
