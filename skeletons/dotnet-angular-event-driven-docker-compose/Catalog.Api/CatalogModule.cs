using Catalog.Application.Commands;
using Catalog.Application.Commands.Handlers;
using Catalog.Application.Contracts;
using Catalog.Application.Models;
using Catalog.Application.Queries;
using Catalog.Application.Queries.Handlers;
using Catalog.Data;
using Common.Lib.Contracts;
using Common.Lib.Results;
using Common.Providers.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Api;

/// <summary>Module self-registration + endpoint mapping, called from the thin
/// API host (adrs/dotnet/thin-api-host.md). Two DbContexts share this host, so
/// each handler is wired to a unit of work bound to ITS OWN context — a shared
/// IUnitOfWork registration would silently save the wrong module's changes.</summary>
public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CatalogDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICommandHandler<CreateProductCommand, Result<Guid>>>(sp =>
            new CreateProductCommandHandler(
                sp.GetRequiredService<IProductRepository>(),
                new EfUnitOfWork<CatalogDbContext>(sp.GetRequiredService<CatalogDbContext>())));
        services.AddScoped<IQueryHandler<GetProductByIdQuery, ProductContext?>, GetProductByIdQueryHandler>();
        services.AddScoped<IQueryHandler<ListProductsQuery, IReadOnlyList<ProductContext>>, ListProductsQueryHandler>();
        return services;
    }

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        // Minimal API endpoints (family-B convention): bare DTOs, Result mapped to
        // Problem Details, nullable query results mapped to 404.
        app.MapPost("/v1/products", async (
            CreateProductCommand command,
            ICommandHandler<CreateProductCommand, Result<Guid>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(command, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/v1/products/{result.Value}", new { id = result.Value })
                : result.Error.ToProblem();
        });

        app.MapGet("/v1/products/{productId:guid}", async (
            Guid productId,
            IQueryHandler<GetProductByIdQuery, ProductContext?> handler,
            CancellationToken cancellationToken) =>
        {
            var product = await handler.HandleAsync(new GetProductByIdQuery(productId), cancellationToken);
            return product is null
                ? Results.Problem(statusCode: 404, title: "Not Found",
                    detail: $"Product {productId} was not found.")
                : Results.Ok(product);
        });

        app.MapGet("/v1/products", async (
            IQueryHandler<ListProductsQuery, IReadOnlyList<ProductContext>> handler,
            CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(new ListProductsQuery(), cancellationToken)));

        return app;
    }
}
