using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Modules.Catalog;
using MyApp.Modules.Identity;
using MyApp.Modules.Identity.Services;
using Xunit;

namespace MyApp.Modules.Catalog.Tests;

/// <summary>Boots the real pipeline against a real PostgreSQL
/// (adrs/dotnet/xunit-per-module-tests.md).</summary>
public sealed class CatalogApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
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
        // Product writes are admin-gated, so catalog tests also need the identity
        // schema and a seeded admin.
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await catalog.Database.EnsureDeletedAsync();
        await catalog.Database.MigrateAsync();
        await identity.Database.MigrateAsync();

        var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        await identityService.CreateUserAsync(
            "admin@example.com", "Admin123!", "admin", TestContext.Current.CancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
