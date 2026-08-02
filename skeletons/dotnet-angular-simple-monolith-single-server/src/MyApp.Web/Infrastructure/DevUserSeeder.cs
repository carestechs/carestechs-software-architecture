using MyApp.Web.Features.Identity;

namespace MyApp.Web.Infrastructure;

/// <summary>Development-only seed users. Skips silently when the database or
/// schema is not ready; never runs outside Development.</summary>
public static class DevUserSeeder
{
    public static async Task TrySeedAsync(IServiceProvider services, ILogger logger)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!await db.Database.CanConnectAsync())
            {
                return;
            }

            var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
            if (!db.Users.Any())
            {
                await identity.CreateUserAsync("admin@example.com", "Admin123!", "admin", CancellationToken.None);
                await identity.CreateUserAsync("agent@example.com", "Agent123!", "agent", CancellationToken.None);
                logger.LogInformation("Seeded dev users admin@example.com / agent@example.com");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Dev user seeding skipped: {Message}", ex.Message);
        }
    }
}
