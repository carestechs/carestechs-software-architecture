using System.ComponentModel.DataAnnotations;

namespace MyApp.Web.Features.Catalog;

public sealed record ProductDto(
    Guid Id, string Sku, string Name, DateTimeOffset CreatedAt, DateTimeOffset? SearchIndexedAt);

public sealed class CreateProductRequest
{
    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}
