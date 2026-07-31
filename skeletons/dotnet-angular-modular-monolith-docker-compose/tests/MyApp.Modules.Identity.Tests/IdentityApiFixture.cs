using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Modules.Catalog;
using MyApp.Modules.Identity;
using MyApp.Modules.Identity.Services;
using Npgsql;
using Xunit;

namespace MyApp.Modules.Identity.Tests;

/// <summary>Boots the real pipeline against a real PostgreSQL
/// (adrs/dotnet/xunit-per-module-tests.md).</summary>
public sealed class IdentityApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string ConnectionString = BuildConnectionString();

    private static string BuildConnectionString()
    {
        var configured = Environment.GetEnvironmentVariable("TEST_DATABASE_URL")
            ?? "Host=localhost;Port=5432;Database=app_test;Username=postgres;Password=postgres";
        // Test assemblies may run in parallel; each module's test project gets its own
        // database so the destructive reset below never races another fixture.
        var builder = new NpgsqlConnectionStringBuilder(configured);
        builder.Database += "_identity";
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
        // The role-ladder test exercises a real catalog write after the auth
        // rungs, so the catalog schema comes up alongside identity.
        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await identityDb.Database.EnsureDeletedAsync();
        await identityDb.Database.MigrateAsync();
        await catalogDb.Database.MigrateAsync();

        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        await identity.CreateUserAsync("admin@example.com", "Admin123!", "admin", TestContext.Current.CancellationToken);
        await identity.CreateUserAsync("agent@example.com", "Agent123!", "agent", TestContext.Current.CancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
