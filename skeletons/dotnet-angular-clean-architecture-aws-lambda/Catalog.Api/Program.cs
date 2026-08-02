using Catalog.Api;
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
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Same Program.cs runs on Kestrel locally and as a Lambda behind API Gateway in
// production (adrs/deployment/aws-lambda-serverless.md) — this call is a no-op
// outside the Lambda runtime.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole();
}

var connectionString = builder.Configuration["DATABASE_URL"]
    ?? "Host=localhost;Port=5432;Database=app;Username=postgres;Password=postgres";
builder.Services.AddDbContext<CatalogDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddProblemDetails();

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork<CatalogDbContext>>();
builder.Services.AddScoped<ICommandHandler<CreateProductCommand, Result<Guid>>, CreateProductCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetProductByIdQuery, ProductContext?>, GetProductByIdQueryHandler>();
builder.Services.AddScoped<IQueryHandler<ListProductsQuery, IReadOnlyList<ProductContext>>, ListProductsQueryHandler>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();

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

app.MapGet("/v1/health", () => Results.Ok(new { status = "ok" }));

await app.RunAsync();

public partial class Program;
