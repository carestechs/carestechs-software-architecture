using System.Text;
using RabbitMQ.Client;

namespace Common.Providers.Queue;

/// <summary>Header plumbing: AMQP header values round-trip as byte arrays, and
/// the broker-maintained x-death header carries the delivery history the
/// consumer uses to budget retries.</summary>
public static class RabbitMqHeaders
{
    public const string Correlation = "correlationid";

    public static string? CorrelationId(IReadOnlyBasicProperties properties) =>
        properties.Headers?.TryGetValue(Correlation, out var value) == true
            ? AsString(value)
            : null;

    /// <summary>How many times this delivery has already been rejected from
    /// <paramref name="queueName"/> (0 on first delivery).</summary>
    public static int RejectedCount(IReadOnlyBasicProperties properties, string queueName)
    {
        if (properties.Headers?.TryGetValue("x-death", out var raw) != true
            || raw is not IEnumerable<object> deaths)
        {
            return 0;
        }

        foreach (var entry in deaths)
        {
            if (entry is not IDictionary<string, object?> death)
            {
                continue;
            }
            death.TryGetValue("queue", out var deathQueue);
            death.TryGetValue("reason", out var reason);
            if (AsString(deathQueue) == queueName && AsString(reason) == "rejected"
                && death.TryGetValue("count", out var count) && count is long rejected)
            {
                return (int)rejected;
            }
        }
        return 0;
    }

    private static string? AsString(object? value) => value switch
    {
        byte[] bytes => Encoding.UTF8.GetString(bytes),
        string text => text,
        _ => null,
    };
}
