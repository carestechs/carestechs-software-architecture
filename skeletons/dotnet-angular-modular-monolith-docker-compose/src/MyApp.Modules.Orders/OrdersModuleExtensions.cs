using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyApp.Contracts.Configuration;
using MyApp.Modules.Orders.Services;

namespace MyApp.Modules.Orders;

/// <summary>Module self-registration (adrs/dotnet/modular-monolith.md, adrs/dotnet/thin-api-host.md).</summary>
public static class OrdersModuleExtensions
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services)
    {
        services.AddDbContext<OrdersDbContext>((provider, options) =>
        {
            var database = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(database.ConnectionString).UseSnakeCaseNamingConvention();
        });
        services.AddScoped<IOrdersService, OrdersService>();
        return services;
    }
}
