using Microsoft.EntityFrameworkCore;
using MyApp.Modules.Orders.Entities;

namespace MyApp.Modules.Orders;

/// <summary>Maps only orders-owned entities (adrs/dotnet/dbcontext-per-module.md).</summary>
public class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().HasIndex(o => o.ProductId);
    }
}
