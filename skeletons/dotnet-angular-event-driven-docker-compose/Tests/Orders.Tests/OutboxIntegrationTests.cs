using System.Text.Json;
using Common.Core.Messages;
using Common.Lib.Contracts;
using Common.Lib.Results;
using Common.Providers.Data;
using Common.Providers.Events;
using Common.Providers.Queue;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orders.Application.Commands;
using Orders.Application.Commands.Handlers;
using Orders.Application.Contracts;
using Orders.Application.Events;
using Orders.Application.Reactors;
using Orders.Data;
using Outbox.Dispatch;
using Xunit;

namespace Orders.Tests;

/// <summary>The transactional outbox, both halves
/// (adrs/database/transactional-outbox.md): placing an order commits the outbox
/// row with it, and the dispatcher publishes pending rows to the real broker,
/// stamping them dispatched.</summary>
public class OutboxIntegrationTests
{
    private static readonly string? BrokerUrl = Environment.GetEnvironmentVariable("RABBITMQ_URL");
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_URL");

    private static ServiceProvider BuildOrderServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(ConnectionString!));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<CorrelationContext>();
        services.AddScoped<ICorrelationContext>(sp => sp.GetRequiredService<CorrelationContext>());
        services.AddScoped<IEventBus, EventBusProvider>();
        services.AddScoped<IReactor<OrderPlacedEvent>, OrderPlacedReactor>();
        services.AddScoped<ICommandHandler<PlaceOrderCommand, Result<Guid>>>(sp =>
            new PlaceOrderCommandHandler(
                sp.GetRequiredService<IOrderRepository>(),
                new EfUnitOfWork<OrdersDbContext>(sp.GetRequiredService<OrdersDbContext>()),
                sp.GetRequiredService<IEventBus>()));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task PlacingAnOrder_CommitsTheOutboxRowWithIt()
    {
        Assert.SkipWhen(ConnectionString is null,
            "TEST_DATABASE_URL not set — integration runs in CI against the Flyway-migrated database.");

        var ct = TestContext.Current.CancellationToken;
        await using var services = BuildOrderServices();
        using var scope = services.CreateScope();

        var correlation = scope.ServiceProvider.GetRequiredService<CorrelationContext>();
        correlation.CorrelationId = $"corr-outbox-{Guid.CreateVersion7():N}";

        var handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<PlaceOrderCommand, Result<Guid>>>();
        var result = await handler.HandleAsync(
            new PlaceOrderCommand(Guid.CreateVersion7(), 3), ct);
        Assert.True(result.IsSuccess);

        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var row = await db.OutboxMessages.SingleAsync(
            m => m.CorrelationId == correlation.CorrelationId, ct);
        Assert.Equal(QueueNames.OrderPlaced, row.QueueName);
        Assert.Null(row.DispatchedAt);
        var payload = JsonSerializer.Deserialize<OrderPlacedMessage>(row.Payload);
        Assert.Equal(result.Value, payload!.OrderId);
    }

    [Fact]
    public async Task Drain_PublishesPendingRowsAndStampsThem()
    {
        Assert.SkipWhen(BrokerUrl is null || ConnectionString is null,
            "RABBITMQ_URL/TEST_DATABASE_URL not set — outbox dispatch integration runs in CI.");

        var ct = TestContext.Current.CancellationToken;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["RABBITMQ_URL"] = BrokerUrl }).Build();
        await using var broker = new RabbitMqConnectionProvider(configuration);
        await using var services = BuildOrderServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        // a pending row aimed at a queue unique to this run
        var queueName = $"it-outbox-{Guid.CreateVersion7():N}";
        var message = new OrderPlacedMessage(Guid.CreateVersion7(), Guid.CreateVersion7(), 7);
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            QueueName = queueName,
            Payload = JsonSerializer.Serialize(message),
            CorrelationId = "corr-drain",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);

        var dispatcher = new OutboxDispatcher(
            db,
            new RabbitMqQueueProvider(broker, configuration),
            scope.ServiceProvider.GetRequiredService<ILogger<OutboxDispatcher>>());
        Assert.True(await dispatcher.DrainOnceAsync(ct) >= 1);

        // the row is stamped...
        var row = await db.OutboxMessages.SingleAsync(m => m.QueueName == queueName, ct);
        Assert.NotNull(row.DispatchedAt);

        // ...and the message is really on the broker, correlation intact
        var connection = await broker.GetConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        RabbitMQ.Client.BasicGetResult? received = null;
        for (var i = 0; i < 40 && received is null; i++)
        {
            received = await channel.BasicGetAsync(queueName, autoAck: true, ct);
            if (received is null)
            {
                await Task.Delay(250, ct);
            }
        }
        Assert.NotNull(received);
        Assert.Equal(message, JsonSerializer.Deserialize<OrderPlacedMessage>(received.Body.Span));
        Assert.Equal("corr-drain", RabbitMqHeaders.CorrelationId(received.BasicProperties));
    }
}
