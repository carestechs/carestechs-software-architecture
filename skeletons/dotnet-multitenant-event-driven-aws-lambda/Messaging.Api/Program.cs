using System.Security.Claims;
using System.Text;
using Amazon.DynamoDBv2;
using Messaging.Application;
using Messaging.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole();
}

// COGNITO STAND-IN (adrs/api/cognito-authentication.md): in production the API
// Gateway authorizer validates Cognito-issued JWTs whose tenant claims were
// stamped by the pre-token trigger. This skeleton validates the same claim
// shape from a configurable test issuer — the claim CONTRACT (org/workspace/sub
// from validated claims only) is what carries over.
var jwtSecret = builder.Configuration["JWT_SECRET"] ?? "dev-only-secret-change-me-minimum-32-bytes!";
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = "skeleton-issuer",
            ValidAudience = "skeleton-platform",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidAlgorithms = ["HS256"],
            ClockSkew = TimeSpan.FromSeconds(60),
            NameClaimType = "sub",
        };
    });
builder.Services.AddAuthorization(options => options.FallbackPolicy = options.DefaultPolicy);

builder.Services.AddProblemDetails();

var dbHost = builder.Configuration["DB_HOST"] ?? "localhost";
var dbPort = int.Parse(builder.Configuration["DB_PORT"] ?? "5432");
var dbUser = builder.Configuration["DB_USER"] ?? "postgres";
var dbPassword = builder.Configuration["DB_PASSWORD"] ?? "postgres";

builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
    builder.Configuration["DDB_ENDPOINT_URL"] is { Length: > 0 } endpoint
        ? new AmazonDynamoDBClient(new AmazonDynamoDBConfig { ServiceURL = endpoint })
        : new AmazonDynamoDBClient());

// module reached ONLY through its facade (adrs/dotnet/module-facade.md)
builder.Services.AddSingleton<IMessagingModuleApi>(sp =>
    MessagingModule.Create(dbHost, dbPort, dbUser, dbPassword,
        sp.GetRequiredService<IAmazonDynamoDB>()));

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

// tenant scope comes from VALIDATED claims — never from the request body
// (adrs/database/database-per-tenant.md)
static (string Org, string Workspace)? TenantOf(ClaimsPrincipal user)
{
    var org = user.FindFirstValue("org");
    var workspace = user.FindFirstValue("workspace");
    return org is null || workspace is null ? null : (org, workspace);
}

app.MapPost("/v1/conversations", async (
    StartConversationRequest request, ClaimsPrincipal user,
    IMessagingModuleApi messaging, CancellationToken cancellationToken) =>
{
    if (TenantOf(user) is not { } tenant)
    {
        return Results.Problem(statusCode: 403, title: "Forbidden", detail: "Token carries no tenant scope.");
    }

    var id = await messaging.StartConversationAsync(
        tenant.Org, tenant.Workspace, request.ContactName, cancellationToken);
    return Results.Created($"/v1/conversations/{id}", new { id });
});

app.MapGet("/v1/conversations/{conversationId:guid}", async (
    Guid conversationId, ClaimsPrincipal user,
    IMessagingModuleApi messaging, CancellationToken cancellationToken) =>
{
    if (TenantOf(user) is not { } tenant)
    {
        return Results.Problem(statusCode: 403, title: "Forbidden", detail: "Token carries no tenant scope.");
    }

    var conversation = await messaging.GetConversationAsync(
        tenant.Org, tenant.Workspace, conversationId, cancellationToken);
    return conversation is null
        ? Results.Problem(statusCode: 404, title: "Not Found",
            detail: $"Conversation {conversationId} was not found.")
        : Results.Ok(conversation);
});

app.MapPost("/v1/conversations/{conversationId:guid}/messages", async (
    Guid conversationId, AppendMessageRequest request, ClaimsPrincipal user, HttpContext http,
    IMessagingModuleApi messaging, CancellationToken cancellationToken) =>
{
    if (TenantOf(user) is not { } tenant)
    {
        return Results.Problem(statusCode: 403, title: "Forbidden", detail: "Token carries no tenant scope.");
    }

    var correlation = http.Request.Headers.TryGetValue("X-Request-ID", out var incoming)
        && !string.IsNullOrWhiteSpace(incoming)
            ? incoming.ToString()
            : Guid.CreateVersion7().ToString("N");
    var appended = await messaging.AppendMessageAsync(
        tenant.Org, tenant.Workspace, conversationId,
        user.FindFirstValue("sub") ?? "unknown", request.Text, correlation, cancellationToken);
    return appended
        ? Results.Accepted()
        : Results.Problem(statusCode: 404, title: "Not Found",
            detail: $"Conversation {conversationId} was not found.");
});

app.MapGet("/v1/conversations/{conversationId:guid}/messages", async (
    Guid conversationId, ClaimsPrincipal user,
    IMessagingModuleApi messaging, CancellationToken cancellationToken) =>
{
    if (TenantOf(user) is not { } tenant)
    {
        return Results.Problem(statusCode: 403, title: "Forbidden", detail: "Token carries no tenant scope.");
    }

    return Results.Ok(await messaging.ListMessagesAsync(
        tenant.Org, tenant.Workspace, conversationId, cancellationToken));
});

app.MapGet("/v1/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

await app.RunAsync();

public sealed record StartConversationRequest(string ContactName);
public sealed record AppendMessageRequest(string Text);

public partial class Program;
