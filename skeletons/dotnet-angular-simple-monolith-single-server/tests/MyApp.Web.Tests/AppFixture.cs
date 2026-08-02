using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Web;
using MyApp.Web.Features.Identity;
using Xunit;

namespace MyApp.Web.Tests;

/// <summary>Boots the real pipeline (including hosted services — the job runner
/// runs for real) against a real PostgreSQL.</summary>
public sealed class AppFixture : WebApplicationFactory<Program>, IAsyncLifetime
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
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        await identity.CreateUserAsync("admin@example.com", "Admin123!", "admin", TestContext.Current.CancellationToken);
        await identity.CreateUserAsync("agent@example.com", "Agent123!", "agent", TestContext.Current.CancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
