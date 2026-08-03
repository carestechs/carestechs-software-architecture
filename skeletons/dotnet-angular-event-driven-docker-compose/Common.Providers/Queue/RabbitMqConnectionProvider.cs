using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Common.Providers.Queue;

/// <summary>One AMQP connection per process, opened lazily so hosts that never
/// touch the broker (the API under test, for example) need no broker at all.
/// Channels are cheap and NOT thread-safe — create one per unit of work.</summary>
public sealed class RabbitMqConnectionProvider(IConfiguration configuration) : IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private IConnection? connection;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (connection is { IsOpen: true })
        {
            return connection;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (connection is not { IsOpen: true })
            {
                var url = configuration["RABBITMQ_URL"] ?? "amqp://guest:guest@localhost:5672";
                var factory = new ConnectionFactory { Uri = new Uri(url) };
                connection = await factory.CreateConnectionAsync(cancellationToken);
            }
            return connection;
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync();
        }
        gate.Dispose();
    }
}
