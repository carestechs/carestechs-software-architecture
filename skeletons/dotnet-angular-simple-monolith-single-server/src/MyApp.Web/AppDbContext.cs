using Microsoft.EntityFrameworkCore;
using MyApp.Web.Features.Catalog;
using MyApp.Web.Features.Identity;

namespace MyApp.Web;

/// <summary>ONE context for the whole app (adrs/dotnet/single-project-monolith.md).
/// Navigation properties between entities are permitted at this rung — there are
/// no module boundaries to protect yet.</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var product = modelBuilder.Entity<Product>();
        product.Property(p => p.Sku).HasMaxLength(64);
        product.HasIndex(p => p.Sku).IsUnique();
        product.Property(p => p.Name).HasMaxLength(200);

        var user = modelBuilder.Entity<User>();
        user.Property(u => u.Email).HasMaxLength(200);
        user.HasIndex(u => u.Email).IsUnique();
        user.Property(u => u.PasswordHash).HasMaxLength(200);
        user.Property(u => u.Role).HasMaxLength(20);

        var token = modelBuilder.Entity<RefreshToken>();
        token.Property(t => t.TokenHash).HasMaxLength(64);
        token.HasIndex(t => t.TokenHash).IsUnique();
        token.HasIndex(t => t.FamilyId);
        token.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
