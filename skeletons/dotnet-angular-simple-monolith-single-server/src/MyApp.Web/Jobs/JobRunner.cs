using Microsoft.EntityFrameworkCore;

namespace MyApp.Web.Jobs;

/// <summary>Hosted consumer (adrs/dotnet/in-process-background-jobs.md): scope per
/// job — never a scoped DbContext captured by this singleton — and the stopping
/// token drains the channel on shutdown. The job here stands in for "sync the
/// product to a search index": tolerable-loss, reconciled by a future re-index.</summary>
public sealed class JobRunner(
    JobQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<JobRunner> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var product = await db.Products.FirstOrDefaultAsync(
                    p => p.Id == job.ProductId, stoppingToken);
                if (product is not null)
                {
                    product.SearchIndexedAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("Indexed product {ProductId}", job.ProductId);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // one failed job never kills the runner; bounded by not re-enqueueing
                logger.LogError(ex, "ProductCreated job failed for {ProductId}", job.ProductId);
            }
        }
    }
}
