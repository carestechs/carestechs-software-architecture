using Amazon.SQS;
using Common.Lib.Contracts;
using Common.Lib.Results;
using Common.Providers.Data;
using Common.Providers.Events;
using Common.Providers.Queue;
using Microsoft.EntityFrameworkCore;
using Orders.Api;
using Orders.Application.Commands;
using Orders.Application.Commands.Handlers;
using Orders.Application.Contracts;
using Orders.Application.Events;
using Orders.Application.Models;
using Orders.Application.Queries;
using Orders.Application.Reactors;
using Orders.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole();
}

var connectionString = builder.Configuration["DATABASE_URL"]
    ?? "Host=localhost;Port=5432;Database=app;Username=postgres;Password=postgres";
builder.Services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddProblemDetails();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork<OrdersDbContext>>();
builder.Services.AddScoped<ICommandHandler<PlaceOrderCommand, Result<Guid>>, PlaceOrderCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetOrderByIdQuery, OrderContext?>, GetOrderByIdQueryHandler>();

// Dev/prod provider split (adrs/deployment/aws-secrets-parameters.md pattern):
// the local HTTP queue server in Development, SQS elsewhere. SQS-compatible
// brokers (ElasticMQ in CI) work through the same provider via AWS_ENDPOINT_URL.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpClient("queue", client =>
        client.BaseAddress = new Uri("http://localhost:9324"));
    builder.Services.AddScoped<IQueueProvider>(sp => new HttpQueueProvider(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("queue")));
}
else
{
    builder.Services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient());
    builder.Services.AddScoped<IQueueProvider, SqsQueueProvider>();
}

builder.Services.AddScoped<CorrelationContext>();
builder.Services.AddScoped<ICorrelationContext>(sp => sp.GetRequiredService<CorrelationContext>());
builder.Services.AddScoped<IEventBus, EventBusProvider>();
builder.Services.AddScoped<IReactor<OrderPlacedEvent>, OrderPlacedReactor>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();

// correlation minted at ingress (adrs/deployment/correlation-propagation.md)
app.Use(async (context, next) =>
{
    var correlation = context.RequestServices.GetRequiredService<CorrelationContext>();
    if (context.Request.Headers.TryGetValue("X-Request-ID", out var incoming)
        && !string.IsNullOrWhiteSpace(incoming))
    {
        correlation.CorrelationId = incoming.ToString();
    }
    context.Response.Headers["X-Request-ID"] = correlation.CorrelationId;
    await next(context);
});

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

app.MapGet("/v1/health", () => Results.Ok(new { status = "ok" }));

await app.RunAsync();

public partial class Program;
