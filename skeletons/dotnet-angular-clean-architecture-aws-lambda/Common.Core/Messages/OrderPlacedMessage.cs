namespace Common.Core.Messages;

/// <summary>Cross-module queue message (adrs/deployment/queue-based-decoupling.md).
/// Carries ids and primitives only — consumers resolve details themselves
/// (adrs/dotnet/cross-module-by-id.md).</summary>
public sealed record OrderPlacedMessage(Guid OrderId, Guid ProductId, int Quantity);

public static class QueueNames
{
    public const string OrderPlaced = "order-placed-queue";
}
