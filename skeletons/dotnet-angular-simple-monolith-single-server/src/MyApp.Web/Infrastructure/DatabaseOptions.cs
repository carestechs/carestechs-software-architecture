using System.ComponentModel.DataAnnotations;

namespace MyApp.Web.Infrastructure;

/// <summary>Typed settings from the environment (adrs/deployment/env-connection-urls.md).</summary>
public sealed class DatabaseOptions
{
    public const string DevelopmentDefault =
        "Host=localhost;Port=5432;Database=app;Username=postgres;Password=postgres";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;
}
