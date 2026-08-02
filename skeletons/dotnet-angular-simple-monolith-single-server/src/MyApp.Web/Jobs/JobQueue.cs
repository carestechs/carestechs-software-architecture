using System.Threading.Channels;

namespace MyApp.Web.Jobs;

public sealed record ProductCreatedJob(Guid ProductId);

/// <summary>Bounded in-process queue (adrs/dotnet/in-process-background-jobs.md).
/// Tolerable-loss work only: contents die with the process — must-survive work
/// belongs in a persistent job store or the queue-based-decoupling rung.</summary>
public sealed class JobQueue
{
    private readonly Channel<ProductCreatedJob> _channel =
        Channel.CreateBounded<ProductCreatedJob>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait, // backpressure, never unbounded memory
        });

    public ChannelReader<ProductCreatedJob> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(ProductCreatedJob job, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(job, cancellationToken);
}
