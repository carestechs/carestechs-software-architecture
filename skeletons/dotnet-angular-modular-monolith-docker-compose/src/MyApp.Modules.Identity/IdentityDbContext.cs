using Microsoft.EntityFrameworkCore;
using MyApp.Modules.Identity.Entities;

namespace MyApp.Modules.Identity;

/// <summary>Maps only identity-owned entities (adrs/dotnet/dbcontext-per-module.md).</summary>
public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
