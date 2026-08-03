using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Outbox.Dispatch;

/// <summary>Interval drain loop — the compose-substrate stand-in for the lambda
/// siblings' scheduled dispatch function.</summary>
public sealed class OutboxDispatchService(
    IServiceProvider services,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollMs = int.TryParse(configuration["OUTBOX_POLL_MS"], out var ms) ? ms : 2000;
        while (!stoppingToken.IsCancellationRequested)
        {
            int drained;
            using (var scope = services.CreateScope())
            {
                drained = await scope.ServiceProvider
                    .GetRequiredService<OutboxDispatcher>()
                    .DrainOnceAsync(stoppingToken);
            }
            // drain again immediately while there is a backlog
            await Task.Delay(drained > 0 ? 100 : pollMs, stoppingToken);
        }
    }
}
