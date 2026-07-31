using Microsoft.EntityFrameworkCore;
using MyApp.Modules.Catalog.Entities;

namespace MyApp.Modules.Catalog;

/// <summary>Maps only catalog-owned entities (adrs/dotnet/dbcontext-per-module.md).</summary>
public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var product = modelBuilder.Entity<Product>();
        product.Property(p => p.Sku).HasMaxLength(64);
        product.HasIndex(p => p.Sku).IsUnique();
        product.Property(p => p.Name).HasMaxLength(200);
    }
}
