using System.Text;
using Common.Core.Messages;
using Common.Providers.Queue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Orders.Worker;

/// <summary>RabbitMQ consumer (adrs/deployment/rabbitmq-broker.md): manual acks,
/// explicit prefetch, and the DLX retry cycle. A failed delivery is nacked
/// without requeue — the topology routes it through the TTL retry queue back
/// here. Once the attempt budget is spent, the message is parked in the DLQ
/// explicitly (publish + ack), because another nack would just keep it cycling.</summary>
public sealed class OrderPlacedConsumer(
    IServiceProvider services,
    RabbitMqConnectionProvider connectionProvider,
    IConfiguration configuration,
    ILogger<OrderPlacedConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = configuration["ORDERS_QUEUE"] ?? QueueNames.OrderPlaced;
        var retryTtl = int.TryParse(configuration["RABBITMQ_RETRY_TTL_MS"], out var ttl) ? ttl : 5000;
        var maxAttempts = int.TryParse(configuration["RABBITMQ_MAX_ATTEMPTS"], out var max) ? max : 3;

        var connection = await connectionProvider.GetConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await RabbitMqTopology.DeclareWorkQueueAsync(channel, queueName, retryTtl, stoppingToken);
        // explicit prefetch — unbounded is forbidden (adrs/deployment/rabbitmq-broker.md)
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(delivery.Body.Span);
                var correlationId = RabbitMqHeaders.CorrelationId(delivery.BasicProperties);

                bool done;
                using (var scope = services.CreateScope())
                {
                    done = await scope.ServiceProvider.GetRequiredService<OrderPlacedProcessor>()
                        .ProcessAsync(body, correlationId, stoppingToken);
                }
                if (done)
                {
                    await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
                    return;
                }

                var attempt = RabbitMqHeaders.RejectedCount(delivery.BasicProperties, queueName) + 1;
                if (attempt >= maxAttempts)
                {
                    logger.LogError("Parking message in the DLQ after {Attempts} attempts", attempt);
                    await channel.BasicPublishAsync(
                        RabbitMqTopology.DeadLetterExchange, routingKey: queueName, mandatory: false,
                        basicProperties: new BasicProperties
                        {
                            Persistent = true,
                            Headers = delivery.BasicProperties.Headers is { } headers
                                ? new Dictionary<string, object?>(headers)
                                : null,
                        },
                        body: delivery.Body, cancellationToken: stoppingToken);
                    await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
                }
                else
                {
                    // no requeue: the DLX retry topology owns the backoff
                    await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false,
                        requeue: false, cancellationToken: stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // never tear down the consume loop; the message takes the retry path
                logger.LogError(ex, "Unhandled failure processing delivery {Tag}", delivery.DeliveryTag);
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false,
                    requeue: false, cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumerTag: "",
            noLocal: false, exclusive: false, arguments: null, consumer: consumer,
            cancellationToken: stoppingToken);
        logger.LogInformation("Consuming {Queue} (prefetch 10, max {Max} attempts)", queueName, maxAttempts);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
    }
}
