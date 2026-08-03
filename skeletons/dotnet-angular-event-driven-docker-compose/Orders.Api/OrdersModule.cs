using Common.Lib.Contracts;
using Common.Lib.Results;
using Common.Providers.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Commands;
using Orders.Application.Commands.Handlers;
using Orders.Application.Contracts;
using Orders.Application.Events;
using Orders.Application.Models;
using Orders.Application.Queries;
using Orders.Application.Reactors;
using Orders.Data;

namespace Orders.Api;

/// <summary>Module self-registration + endpoint mapping, called from the thin
/// API host (adrs/dotnet/thin-api-host.md). Handlers get a unit of work bound
/// to the Orders context (see CatalogModule for why that wiring is explicit).</summary>
public static class OrdersModule
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IReactor<OrderPlacedEvent>, OrderPlacedReactor>();
        services.AddScoped<ICommandHandler<PlaceOrderCommand, Result<Guid>>>(sp =>
            new PlaceOrderCommandHandler(
                sp.GetRequiredService<IOrderRepository>(),
                new EfUnitOfWork<OrdersDbContext>(sp.GetRequiredService<OrdersDbContext>()),
                sp.GetRequiredService<IEventBus>()));
        services.AddScoped<IQueryHandler<GetOrderByIdQuery, OrderContext?>, GetOrderByIdQueryHandler>();
        return services;
    }

    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/orders", async (
            PlaceOrderCommand command,
            ICommandHandler<PlaceOrderCommand, Result<Guid>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(command, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/v1/orders/{result.Value}", new { id = result.Value })
                : result.Error.ToProblem();
        });

        app.MapGet("/v1/orders/{orderId:guid}", async (
            Guid orderId,
            IQueryHandler<GetOrderByIdQuery, OrderContext?> handler,
            CancellationToken cancellationToken) =>
        {
            var order = await handler.HandleAsync(new GetOrderByIdQuery(orderId), cancellationToken);
            return order is null
                ? Results.Problem(statusCode: 404, title: "Not Found",
                    detail: $"Order {orderId} was not found.")
                : Results.Ok(order);
        });

        return app;
    }
}
