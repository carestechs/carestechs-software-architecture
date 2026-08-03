using System.Text.Json;
using Common.Core.Messages;
using Common.Lib.Contracts;
using Common.Lib.Errors;
using Common.Lib.Results;
using Microsoft.Extensions.Logging;
using Orders.Application.Commands;

namespace Orders.Worker;

/// <summary>Message-processing core, kept free of broker plumbing so tests can
/// drive it directly. Restores the correlation id into the log scope before any
/// business logging (adrs/deployment/correlation-propagation.md).</summary>
public sealed class OrderPlacedProcessor(
    ICommandHandler<ConfirmOrderCommand, Result<Guid>> confirmHandler,
    ILogger<OrderPlacedProcessor> logger)
{
    /// <returns>true when the message is done; false when it should be retried
    /// (the consumer nacks it into the DLX retry cycle).</returns>
    public async Task<bool> ProcessAsync(
        string body, string? correlationId, CancellationToken cancellationToken)
    {
        var effectiveCorrelation = string.IsNullOrWhiteSpace(correlationId)
            ? $"orphan-{Guid.CreateVersion7():N}" // never silently re-mint (correlation-propagation)
            : correlationId;

        using var _ = logger.BeginScope(
            new Dictionary<string, object> { ["CorrelationId"] = effectiveCorrelation });

        var message = JsonSerializer.Deserialize<OrderPlacedMessage>(body);
        if (message is null)
        {
            // permanent failure: park immediately, don't burn retries
            // (adrs/deployment/idempotent-queue-consumers.md)
            logger.LogError("Unparseable OrderPlaced message; sending to the DLQ path");
            return true;
        }

        var result = await confirmHandler.HandleAsync(
            new ConfirmOrderCommand(message.OrderId), cancellationToken);
        if (result.IsFailure && result.Error.Type == ErrorType.NotFound)
        {
            // transient: the placing transaction may not be visible yet — retry
            logger.LogWarning("Order {OrderId} not found yet; will retry", message.OrderId);
            return false;
        }

        logger.LogInformation("Order {OrderId} confirmed", message.OrderId);
        return true;
    }
}
