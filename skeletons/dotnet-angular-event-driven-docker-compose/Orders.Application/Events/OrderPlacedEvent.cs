using Common.Lib.Contracts;

namespace Orders.Application.Events;

/// <summary>In-process domain event; the reactor turns it into cross-module
/// queue work (adrs/dotnet/event-driven-reactors.md).</summary>
public sealed record OrderPlacedEvent(Guid OrderId, Guid ProductId, int Quantity) : IEvent;
