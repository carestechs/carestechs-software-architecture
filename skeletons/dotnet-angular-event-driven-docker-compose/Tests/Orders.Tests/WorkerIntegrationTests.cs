using System.Text.Json;
using Common.Core.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Data;
using Orders.Domain.Models;
using Orders.Worker;
using Xunit;

namespace Orders.Tests;

/// <summary>Executes the worker's real processing core against the
/// Flyway-migrated PostgreSQL: at-least-once means processing the same message
/// twice must have one effect (adrs/deployment/idempotent-queue-consumers.md).</summary>
public class WorkerIntegrationTests
{
    private static readonly string? ConnectionString =
        Environment.GetEnvironmentVariable("TEST_DATABASE_URL");

    [Fact]
    public async Task ProcessingTheSameMessageTwice_ConfirmsExactlyOnce()
    {
        Assert.SkipWhen(ConnectionString is null,
            "TEST_DATABASE_URL not set — integration runs in CI against the Flyway-migrated database.");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DATABASE_URL"] = ConnectionString })
            .Build();
        await using var services = WorkerServices.Build(configuration);

        Guid orderId;
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var order = Order.Create(Guid.CreateVersion7(), 2);
            db.Orders.Add(order);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            orderId = order.Id;
        }

        var body = JsonSerializer.Serialize(new OrderPlacedMessage(orderId, Guid.CreateVersion7(), 2));

        using (var scope = services.CreateScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<OrderPlacedProcessor>();
            Assert.True(await processor.ProcessAsync(body, "corr-1", TestContext.Current.CancellationToken));
        }

        // read back through a FRESH context: timestamptz stores microseconds, so a
        // tracked in-memory value (100ns ticks) would differ from its own round-trip
        DateTimeOffset? firstConfirmation;
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var confirmed = await db.Orders.FindAsync([orderId], TestContext.Current.CancellationToken);
            Assert.Equal(OrderStatus.Confirmed, confirmed!.Status);
            firstConfirmation = confirmed.ConfirmedAt;
        }

        using (var scope = services.CreateScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<OrderPlacedProcessor>();
            // redelivery: succeeds without a second effect
            Assert.True(await processor.ProcessAsync(body, "corr-1", TestContext.Current.CancellationToken));
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var confirmed = await db.Orders.FindAsync([orderId], TestContext.Current.CancellationToken);
            Assert.Equal(firstConfirmation, confirmed!.ConfirmedAt);
        }
    }

    [Fact]
    public async Task MissingOrder_RequestsRetry()
    {
        Assert.SkipWhen(ConnectionString is null,
            "TEST_DATABASE_URL not set — integration runs in CI against the Flyway-migrated database.");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DATABASE_URL"] = ConnectionString })
            .Build();
        await using var services = WorkerServices.Build(configuration);
        using var scope = services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<OrderPlacedProcessor>();

        var body = JsonSerializer.Serialize(
            new OrderPlacedMessage(Guid.CreateVersion7(), Guid.CreateVersion7(), 1));
        // transient not-found -> false -> the consumer nacks into the DLX retry cycle
        Assert.False(await processor.ProcessAsync(body, "corr-2", TestContext.Current.CancellationToken));
    }
}
