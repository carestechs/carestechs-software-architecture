using System.Text.Json;
using Common.Core.Messages;
using Common.Providers.Queue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Orders.Data;
using Orders.Domain.Models;
using Orders.Worker;
using RabbitMQ.Client;
using Xunit;

namespace Orders.Tests;

/// <summary>Runs the REAL consumer against the real broker and the
/// Flyway-migrated database: manual acks on success, and the DLX retry cycle
/// parking a poison message in the DLQ once the attempt budget is spent
/// (adrs/deployment/rabbitmq-broker.md, adrs/deployment/idempotent-queue-consumers.md).
/// Each test uses its own queue, so the two consumers never see each other.</summary>
public class ConsumerIntegrationTests
{
    private static readonly string? BrokerUrl = Environment.GetEnvironmentVariable("RABBITMQ_URL");
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_URL");

    private static (ServiceProvider Services, RabbitMqConnectionProvider Broker, IConfiguration Config,
        OrderPlacedConsumer Consumer, RabbitMqQueueProvider Publisher) BuildConsumer(string queueName)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["DATABASE_URL"] = ConnectionString,
                ["RABBITMQ_URL"] = BrokerUrl,
                ["ORDERS_QUEUE"] = queueName,
                ["RABBITMQ_RETRY_TTL_MS"] = "400",
                ["RABBITMQ_MAX_ATTEMPTS"] = "2",
            }).Build();
        var services = WorkerServices.Build(configuration);
        var broker = new RabbitMqConnectionProvider(configuration);
        var consumer = new OrderPlacedConsumer(
            services, broker, configuration, NullLogger<OrderPlacedConsumer>.Instance);
        return (services, broker, configuration, consumer, new RabbitMqQueueProvider(broker, configuration));
    }

    [Fact]
    public async Task ConsumesAndConfirms_ThenAcks()
    {
        Assert.SkipWhen(BrokerUrl is null || ConnectionString is null,
            "RABBITMQ_URL/TEST_DATABASE_URL not set — consumer integration runs in CI.");

        var ct = TestContext.Current.CancellationToken;
        var queueName = $"it-consume-{Guid.CreateVersion7():N}";
        var (services, broker, _, consumer, publisher) = BuildConsumer(queueName);
        await using var _ = services;
        await using var __ = broker;

        Guid orderId;
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var order = Order.Create(Guid.CreateVersion7(), 2);
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);
            orderId = order.Id;
        }

        await publisher.EnqueueAsync(queueName,
            new OrderPlacedMessage(orderId, Guid.CreateVersion7(), 2), "corr-consume", ct);

        await consumer.StartAsync(ct);
        try
        {
            for (var i = 0; i < 50; i++)
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
                var current = await db.Orders.FindAsync([orderId], ct);
                if (current!.Status == OrderStatus.Confirmed)
                {
                    return; // consumed, confirmed, acked
                }
                await Task.Delay(300, ct);
            }
            Assert.Fail("The consumer never confirmed the order.");
        }
        finally
        {
            await consumer.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task PoisonMessage_IsParkedInTheDlqAfterMaxAttempts()
    {
        Assert.SkipWhen(BrokerUrl is null || ConnectionString is null,
            "RABBITMQ_URL/TEST_DATABASE_URL not set — consumer integration runs in CI.");

        var ct = TestContext.Current.CancellationToken;
        var queueName = $"it-poison-{Guid.CreateVersion7():N}";
        var (services, broker, _, consumer, publisher) = BuildConsumer(queueName);
        await using var _ = services;
        await using var __ = broker;

        // an order that will never exist: the processor reports a transient
        // failure every time, so the message cycles work -> retry(TTL) -> work
        // until the budget (2 attempts) parks it
        var poison = new OrderPlacedMessage(Guid.CreateVersion7(), Guid.CreateVersion7(), 1);
        await publisher.EnqueueAsync(queueName, poison, "corr-poison", ct);

        await consumer.StartAsync(ct);
        try
        {
            var connection = await broker.GetConnectionAsync(ct);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
            for (var i = 0; i < 50; i++)
            {
                var dead = await channel.BasicGetAsync(
                    queueName + RabbitMqTopology.DeadLetterSuffix, autoAck: true, ct);
                if (dead is not null)
                {
                    Assert.Equal(poison, JsonSerializer.Deserialize<OrderPlacedMessage>(dead.Body.Span));
                    return;
                }
                await Task.Delay(300, ct);
            }
            Assert.Fail("The poison message never reached the DLQ.");
        }
        finally
        {
            await consumer.StopAsync(CancellationToken.None);
        }
    }
}
