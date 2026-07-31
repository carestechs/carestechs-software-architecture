using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyApp.Contracts.Configuration;
using MyApp.Modules.Identity.Entities;
using MyApp.Modules.Identity.Services;

namespace MyApp.Modules.Identity;

/// <summary>Module self-registration (adrs/dotnet/modular-monolith.md, adrs/dotnet/thin-api-host.md).</summary>
public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddDbContext<IdentityDbContext>((provider, options) =>
        {
            var database = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(database.ConnectionString).UseSnakeCaseNamingConvention();
        });
        // PBKDF2 hasher from the shared framework — never a homegrown scheme
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<TokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        return services;
    }
}
