using System.ComponentModel.DataAnnotations;

namespace MyApp.Contracts.Configuration;

/// <summary>Typed database settings validated at startup (adrs/deployment/env-connection-urls.md).</summary>
public sealed class DatabaseOptions
{
    public const string DevelopmentDefault =
        "Host=localhost;Port=5432;Database=app;Username=postgres;Password=postgres";

    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = string.Empty;
}
