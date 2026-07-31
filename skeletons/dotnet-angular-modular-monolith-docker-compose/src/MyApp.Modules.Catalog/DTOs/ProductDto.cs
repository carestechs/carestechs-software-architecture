namespace MyApp.Modules.Catalog.DTOs;

/// <summary>Response DTO — EF entities never cross the API boundary (adrs/dotnet/dto-at-boundary.md).</summary>
public sealed record ProductDto(Guid Id, string Sku, string Name, DateTimeOffset CreatedAt);
