using Catalog.Api;
using Common.Lib.Contracts;
using Common.Providers.Events;
using Orders.Api;

// Thin API host (adrs/dotnet/thin-api-host.md): composition root only. Modules
// self-register; endpoints come from each module's Api library. Note what is
// NOT here: a broker client. The API writes outbox rows inside its transactions
// (adrs/database/transactional-outbox.md); only Outbox.Dispatch and the workers
// talk to RabbitMQ.
var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole();
}

var connectionString = builder.Configuration["DATABASE_URL"]
    ?? "Host=localhost;Port=5432;Database=app;Username=postgres;Password=postgres";

builder.Services.AddProblemDetails();
builder.Services.AddScoped<CorrelationContext>();
builder.Services.AddScoped<ICorrelationContext>(sp => sp.GetRequiredService<CorrelationContext>());
builder.Services.AddScoped<IEventBus, EventBusProvider>();
builder.Services.AddCatalogModule(connectionString);
builder.Services.AddOrdersModule(connectionString);

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

app.MapCatalogEndpoints();
app.MapOrdersEndpoints();
app.MapGet("/v1/health", () => Results.Ok(new { status = "ok" }));

await app.RunAsync();

public partial class Program;
