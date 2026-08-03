using System.Text.Json;
using Common.Core.Messages;
using Common.Providers.Queue;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Xunit;

namespace Orders.Tests;

/// <summary>Round-trips the production RabbitMqQueueProvider against a real
/// broker: durable publish, correlation header survival, and the DLX topology
/// declared as code (adrs/deployment/rabbitmq-broker.md). Uses a unique queue
/// per run so parallel test classes never share broker state.</summary>
public class RabbitMqProviderIntegrationTests
{
    private static readonly string? BrokerUrl =
        Environment.GetEnvironmentVariable("RABBITMQ_URL");

    [Fact]
    public async Task Enqueue_RoundTripsPayloadCorrelationAndTopology()
    {
        Assert.SkipWhen(BrokerUrl is null,
            "RABBITMQ_URL not set — broker integration runs in CI against RabbitMQ.");

        var ct = TestContext.Current.CancellationToken;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["RABBITMQ_URL"] = BrokerUrl }).Build();
        await using var connectionProvider = new RabbitMqConnectionProvider(configuration);

        var queueName = $"it-{Guid.CreateVersion7():N}";
        var provider = new RabbitMqQueueProvider(connectionProvider, configuration);
        var message = new OrderPlacedMessage(Guid.CreateVersion7(), Guid.CreateVersion7(), 5);
        await provider.EnqueueAsync(queueName, message, "corr-xyz", ct);

        var connection = await connectionProvider.GetConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        BasicGetResult? received = null;
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
        Assert.Equal("corr-xyz", RabbitMqHeaders.CorrelationId(received.BasicProperties));

        // the provider declared the whole retry/DLQ topology on the way (passive = throws if absent)
        await channel.QueueDeclarePassiveAsync(queueName + RabbitMqTopology.RetrySuffix, ct);
        await channel.QueueDeclarePassiveAsync(queueName + RabbitMqTopology.DeadLetterSuffix, ct);
    }
}
