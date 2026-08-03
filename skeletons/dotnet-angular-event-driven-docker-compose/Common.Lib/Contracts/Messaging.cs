namespace Common.Lib.Contracts;

/// <summary>CQRS contracts (adrs/dotnet/cqrs-handlers.md): explicit operations,
/// one handler per command/query, no MediatR.</summary>
public interface ICommand<TResult>;

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface IQuery<TResult>;

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

/// <summary>In-process domain event (adrs/dotnet/event-driven-reactors.md).</summary>
public interface IEvent;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken)
        where TEvent : IEvent;
}

/// <summary>Side-effect subscriber; failures are isolated by the bus.</summary>
public interface IReactor<in TEvent> where TEvent : IEvent
{
    Task ReactAsync(TEvent domainEvent, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
