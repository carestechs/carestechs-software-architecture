using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Modules.Catalog;
using MyApp.Modules.Identity;
using MyApp.Modules.Identity.Services;
using Npgsql;
using Xunit;

namespace MyApp.Modules.Orders.Tests;

/// <summary>Boots the real pipeline against a real PostgreSQL
/// (adrs/dotnet/xunit-per-module-tests.md).</summary>
public sealed class OrdersApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string ConnectionString = BuildConnectionString();

    public Guid AdminId { get; private set; }
    public Guid AgentId { get; private set; }
    public Guid Agent2Id { get; private set; }

    private static string BuildConnectionString()
    {
        var configured = Environment.GetEnvironmentVariable("TEST_DATABASE_URL")
            ?? "Host=localhost;Port=5432;Database=app_test;Username=postgres;Password=postgres";
        // Test assemblies may run in parallel; each module's test project gets its own
        // database so the destructive reset below never races another fixture.
        var builder = new NpgsqlConnectionStringBuilder(configured);
        builder.Database += "_orders";
        return builder.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("DATABASE_URL", ConnectionString);
        builder.UseEnvironment("Development");
    }

    public async ValueTask InitializeAsync()
    {
        using var scope = Services.CreateScope();
        // Orders tests drive the API through the front door — creating products
        // (admin) and orders (agents) — so all three schemas come up and users
        // are seeded.
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var orders = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await catalog.Database.EnsureDeletedAsync();
        await catalog.Database.MigrateAsync();
        await orders.Database.MigrateAsync();
        await identity.Database.MigrateAsync();

        var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        var ct = TestContext.Current.CancellationToken;
        AdminId = await identityService.CreateUserAsync("admin@example.com", "Admin123!", "admin", ct);
        AgentId = await identityService.CreateUserAsync("agent@example.com", "Agent123!", "agent", ct);
        Agent2Id = await identityService.CreateUserAsync("agent2@example.com", "Agent123!", "agent", ct);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
