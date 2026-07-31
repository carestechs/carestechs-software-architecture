namespace MyApp.Modules.Catalog.Entities;

public class Product
{
    // UUIDv7 for index locality (adrs/database/uuid-primary-keys.md)
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    // timestamptz via DateTimeOffset, always UTC (adrs/database/timestamptz-always.md)
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
