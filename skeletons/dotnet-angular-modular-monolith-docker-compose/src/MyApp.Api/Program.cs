using MyApp.Api.Infrastructure;
using MyApp.Contracts;
using MyApp.Contracts.Configuration;
using MyApp.Modules.Catalog;

var builder = WebApplication.CreateBuilder(args);

// Structured JSON logs outside Development (adrs/dotnet/structured-logging.md)
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole();
}

// Typed settings from the environment, validated at startup (adrs/deployment/env-connection-urls.md)
builder.Services.AddOptions<DatabaseOptions>()
    .Configure(options => options.ConnectionString =
        builder.Configuration["DATABASE_URL"] ?? DatabaseOptions.DevelopmentDefault)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Problem Details + global exception handler (adrs/dotnet/rfc7807-errors.md)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();

// Modules self-register; the host stays a composition root (adrs/dotnet/thin-api-host.md)
builder.Services.AddCatalogModule();

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<CorrelationMiddleware>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new ApiResponse<object>(new { Status = "ok" })));

app.Run();

public partial class Program;
