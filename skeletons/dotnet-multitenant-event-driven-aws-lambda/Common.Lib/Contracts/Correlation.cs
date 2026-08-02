namespace Common.Lib.Contracts;

/// <summary>One correlation id from ingress through every queue hop
/// (adrs/deployment/correlation-propagation.md). Minted only at true ingress;
/// workers restore it from the message attribute.</summary>
public interface ICorrelationContext
{
    string CorrelationId { get; }
}

public sealed class CorrelationContext : ICorrelationContext
{
    public string CorrelationId { get; set; } = Guid.CreateVersion7().ToString("N");
}
