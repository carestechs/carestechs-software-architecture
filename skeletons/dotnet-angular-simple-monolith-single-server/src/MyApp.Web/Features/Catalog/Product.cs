namespace MyApp.Web.Features.Catalog;

public class Product
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Stamped by the in-process background job — the observable proof
    /// that the channel + hosted service pipeline ran.</summary>
    public DateTimeOffset? SearchIndexedAt { get; set; }
}
