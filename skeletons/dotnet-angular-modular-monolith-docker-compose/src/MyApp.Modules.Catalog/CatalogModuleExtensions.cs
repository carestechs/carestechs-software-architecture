using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyApp.Contracts.Configuration;
using MyApp.Modules.Catalog.Services;

namespace MyApp.Modules.Catalog;

/// <summary>Module self-registration (adrs/dotnet/modular-monolith.md, adrs/dotnet/thin-api-host.md).</summary>
public static class CatalogModuleExtensions
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        services.AddDbContext<CatalogDbContext>((provider, options) =>
        {
            var database = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            // snake_case identifiers via the naming convention package (adrs/database/snake-case-naming.md)
            options.UseNpgsql(database.ConnectionString).UseSnakeCaseNamingConvention();
        });
        services.AddScoped<CatalogService>();
        services.AddScoped<ICatalogService>(provider => provider.GetRequiredService<CatalogService>());
        services.AddScoped<Contracts.ICatalogService>(provider => provider.GetRequiredService<CatalogService>());
        return services;
    }
}
