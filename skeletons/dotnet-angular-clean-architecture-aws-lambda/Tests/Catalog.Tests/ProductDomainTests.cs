using Catalog.Domain.Models;
using Xunit;

namespace Catalog.Tests;

/// <summary>Pure domain tests — run everywhere, no infrastructure
/// (adrs/dotnet/rich-domain-entities.md).</summary>
public class ProductDomainTests
{
    [Fact]
    public void Create_EnforcesInvariantsInTheEntity()
    {
        var product = Product.Create("  sku-9 ", "  Widget  ");
        Assert.Equal("SKU-9", product.Sku); // trimmed + uppercased by the factory
        Assert.Equal("Widget", product.Name);
        Assert.NotEqual(Guid.Empty, product.Id);
    }

    [Theory]
    [InlineData("", "Widget")]
    [InlineData("SKU", "")]
    public void Create_RejectsInvalidInput(string sku, string name)
    {
        Assert.ThrowsAny<ArgumentException>(() => Product.Create(sku, name));
    }
}
