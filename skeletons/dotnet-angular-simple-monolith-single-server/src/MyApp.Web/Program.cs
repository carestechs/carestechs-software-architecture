using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyApp.Web;
using MyApp.Web.Features.Catalog;
using MyApp.Web.Features.Identity;
using MyApp.Web.Infrastructure;
using MyApp.Web.Jobs;

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

// ONE DbContext for the whole app (adrs/dotnet/single-project-monolith.md)
builder.Services.AddDbContext<AppDbContext>((provider, options) =>
{
    var connectionString = builder.Configuration["DATABASE_URL"] ?? DatabaseOptions.DevelopmentDefault;
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
});

// Problem Details + global exception handler (adrs/dotnet/rfc7807-errors.md)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Token validation: explicit algorithm allowlist, issuer, audience, bounded skew
// (adrs/api/jwt-bearer-auth.md); 401/403 stay Problem Details via the events.
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
            ValidAlgorithms = ["HS256"],
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

// Deny by default (adrs/api/role-based-authorization.md)
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddControllers();

// Feature services — the composition root registers them directly; there are no
// modules at this rung (adrs/dotnet/single-project-monolith.md)
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Identity.IPasswordHasher<User>,
    Microsoft.AspNetCore.Identity.PasswordHasher<User>>();

// In-process background jobs: bounded channel + hosted consumer
// (adrs/dotnet/in-process-background-jobs.md)
builder.Services.AddSingleton<JobQueue>();
builder.Services.AddHostedService<JobRunner>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<CorrelationMiddleware>();

// SPA served by the API host (adrs/deployment/spa-served-by-api.md): static
// files + fallback AFTER API routes; unknown /api paths stay Problem Details.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new ApiResponse<object>(new { Status = "ok" })))
    .AllowAnonymous();

app.MapFallback("/api/{**slug}", () => Results.Problem(
        statusCode: 404, title: "Not Found", detail: "Unknown API route."))
    .AllowAnonymous();
app.MapFallbackToFile("index.html").AllowAnonymous();

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
