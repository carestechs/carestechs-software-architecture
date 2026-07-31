using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MyApp.Api.Infrastructure;
using MyApp.Contracts;
using MyApp.Contracts.Configuration;
using MyApp.Modules.Catalog;
using MyApp.Modules.Identity;
using MyApp.Modules.Orders;

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

var jwtSecret = builder.Configuration["JWT_SECRET"] ?? JwtOptions.DevelopmentDefault;
builder.Services.AddOptions<JwtOptions>()
    .Configure(options => options.Secret = jwtSecret)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Problem Details + global exception handler (adrs/dotnet/rfc7807-errors.md)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Token validation: explicit algorithm allowlist, issuer, audience, bounded
// clock skew (adrs/api/jwt-bearer-auth.md). The 401/403 responses stay
// Problem Details via the events below (adrs/dotnet/rfc7807-errors.md).
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep raw claim names: sub, role
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = JwtOptions.Issuer,
            ValidAudience = JwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidAlgorithms = ["HS256"], // never trust the token header's alg
            ClockSkew = TimeSpan.FromSeconds(60),
            NameClaimType = "sub",
            RoleClaimType = "role",
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.Headers.WWWAuthenticate = "Bearer";
                await WriteProblem(context.HttpContext, 401, "Unauthorized",
                    "The access token is missing, expired, or invalid.");
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = 403;
                await WriteProblem(context.HttpContext, 403, "Forbidden",
                    "This action requires a role you do not have.");
            },
        };
    });

// Deny by default: an endpoint without an explicit authorization declaration
// does not ship (adrs/api/role-based-authorization.md)
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddControllers();

// Modules self-register; the host stays a composition root (adrs/dotnet/thin-api-host.md).
// Orders consumes the catalog only through MyApp.Contracts.ICatalogService, which
// AddCatalogModule registers (adrs/dotnet/cross-module-by-id.md).
builder.Services.AddCatalogModule();
builder.Services.AddOrdersModule();
builder.Services.AddIdentityModule();

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<CorrelationMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new ApiResponse<object>(new { Status = "ok" })))
    .AllowAnonymous(); // explicit opt-out, required by the deny-by-default policy

if (app.Environment.IsDevelopment())
{
    await DevUserSeeder.TrySeedAsync(app.Services, app.Logger);
}

await app.RunAsync();

static async Task WriteProblem(HttpContext httpContext, int status, string title, string detail)
{
    var problemDetailsService = httpContext.RequestServices
        .GetRequiredService<IProblemDetailsService>();
    await problemDetailsService.WriteAsync(new ProblemDetailsContext
    {
        HttpContext = httpContext,
        ProblemDetails = new() { Status = status, Title = title, Detail = detail },
    });
}

public partial class Program;
