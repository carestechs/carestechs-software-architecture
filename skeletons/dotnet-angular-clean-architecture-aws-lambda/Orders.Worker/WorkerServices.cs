using Common.Lib.Contracts;
using Common.Lib.Results;
using Common.Providers.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orders.Application.Commands;
using Orders.Application.Commands.Handlers;
using Orders.Application.Contracts;
using Orders.Data;

namespace Orders.Worker;

public static class WorkerServices
{
    public static ServiceProvider Build(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddJsonConsole());

        var connectionString = configuration["DATABASE_URL"]
            ?? "Host=localhost;Port=5432;Database=app;Username=postgres;Password=postgres";
        services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork<OrdersDbContext>>();
        services.AddScoped<ICommandHandler<ConfirmOrderCommand, Result<Guid>>, ConfirmOrderCommandHandler>();
        services.AddScoped<OrderPlacedProcessor>();
        return services.BuildServiceProvider();
    }
}
