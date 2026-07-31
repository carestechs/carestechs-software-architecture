using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Modules.Catalog;
using Xunit;

namespace MyApp.Modules.Orders.Tests;

/// <summary>Boots the real pipeline against a real PostgreSQL
/// (adrs/dotnet/xunit-per-module-tests.md).</summary>
public sealed class OrdersApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("TEST_DATABASE_URL")
        ?? "Host=localhost;Port=5432;Database=app_test;Username=postgres;Password=postgres";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("DATABASE_URL", ConnectionString);
        builder.UseEnvironment("Development");
    }

    public async ValueTask InitializeAsync()
    {
        using var scope = Services.CreateScope();
        // Orders tests drive the API through the front door, which includes creating
        // products — reset the database and bring both modules' schemas up.
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var orders = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await catalog.Database.EnsureDeletedAsync();
        await catalog.Database.MigrateAsync();
        await orders.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
