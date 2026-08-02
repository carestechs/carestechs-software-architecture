using Messaging.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Messaging.Data;

public sealed class OutboxRow
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string QueueName { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DispatchedAt { get; set; }
}

/// <summary>Maps only messaging-owned tables, all inside the module's schema
/// (adrs/database/schema-per-module.md); EF is runtime-only against the
/// DbUp-managed DDL (adrs/deployment/dbup-migrations.md).</summary>
public class MessagingDbContext(DbContextOptions<MessagingDbContext> options) : DbContext(options)
{
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<OutboxRow> Outbox => Set<OutboxRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("messaging");
        modelBuilder.Entity<Conversation>().ToTable("conversations");
        modelBuilder.Entity<OutboxRow>().ToTable("outbox");

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.Name.ToLowerInvariant());
            }
        }
    }
}
