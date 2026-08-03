using Catalog.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Data;

/// <summary>Maps only catalog-owned tables against the Flyway-managed schema —
/// EF Core is a runtime-only ORM here (adrs/deployment/flyway-migrations.md).
/// Lowercase identifiers via the OnModelCreating loop
/// (adrs/database/lowercase-naming.md).</summary>
public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog"); // adrs/database/schema-per-module.md
        modelBuilder.Entity<Product>(product =>
        {
            product.ToTable("products");
            product.Property(p => p.Sku).HasMaxLength(64);
            product.HasIndex(p => p.Sku).IsUnique();
            product.Property(p => p.Name).HasMaxLength(200);
        });

        // lowercase every table and column name — no naming convention package
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.GetTableName()!.ToLowerInvariant());
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.Name.ToLowerInvariant());
            }
        }
    }
}
