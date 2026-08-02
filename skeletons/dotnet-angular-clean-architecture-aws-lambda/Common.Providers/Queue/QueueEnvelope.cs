namespace Common.Providers.Queue;

/// <summary>Wire envelope: payload plus the correlation id that MUST ride every
/// hop (adrs/deployment/correlation-propagation.md).</summary>
public sealed record QueueEnvelope(string CorrelationId, string Payload);
