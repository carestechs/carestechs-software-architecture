using Common.Lib.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Common.Providers.Events;

/// <summary>In-process bus (adrs/dotnet/event-driven-reactors.md): reactors run
/// sequentially and a failing reactor never breaks the publisher or its siblings.</summary>
public sealed class EventBusProvider(
    IServiceProvider serviceProvider,
    ILogger<EventBusProvider> logger) : IEventBus
{
    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        foreach (var reactor in serviceProvider.GetServices<IReactor<TEvent>>())
        {
            try
            {
                await reactor.ReactAsync(domainEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reactor {Reactor} failed for {Event}",
                    reactor.GetType().Name, typeof(TEvent).Name);
            }
        }
    }
}
