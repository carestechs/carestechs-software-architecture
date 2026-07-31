namespace MyApp.Modules.Orders.DTOs;

/// <summary>ProductName is resolved through the catalog contract at read time —
/// never stored on the order row and never joined (adrs/dotnet/cross-module-by-id.md).</summary>
public sealed record OrderDto(
    Guid Id, Guid ProductId, string? ProductName, Guid CreatedBy, int Quantity, DateTimeOffset CreatedAt);
