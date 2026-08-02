using Amazon.SQS;
using Amazon.SQS.Model;
using Common.Lib.Tenancy;
using Common.Providers.Queue;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Outbox.Dispatch;

/// <summary>Drains a tenant's outbox to SQS and marks rows dispatched — the
/// at-least-once correctness path (adrs/database/transactional-outbox.md).
/// Correlation ids persisted with the row ride out as message attributes
/// (adrs/deployment/correlation-propagation.md).</summary>
public sealed class OutboxDispatcher(
    IAmazonSQS sqs,
    string dbHost, int dbPort, string dbUser, string dbPassword,
    ILogger<OutboxDispatcher> logger)
{
    public async Task<int> DrainTenantAsync(
        OrgId org, WorkspaceId workspace, CancellationToken cancellationToken)
    {
        var connectionString = TenantDbConnectionBuilder.BuildConnectionString(
            dbHost, dbPort, dbUser, dbPassword, org, workspace);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var select = new NpgsqlCommand(
            """
            SELECT id, queuename, payload, correlationid FROM messaging.outbox
            WHERE dispatchedat IS NULL
            ORDER BY createdat
            LIMIT 50
            FOR UPDATE SKIP LOCKED
            """, connection);

        var rows = new List<(Guid Id, string Queue, string Payload, string Correlation)>();
        await using (var reader = await select.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add((reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
            }
        }

        foreach (var row in rows)
        {
            var queueUrl = (await sqs.GetQueueUrlAsync(row.Queue, cancellationToken)).QueueUrl;
            await sqs.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = row.Payload,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    [SqsQueueProvider.CorrelationAttribute] = new()
                    {
                        DataType = "String",
                        StringValue = row.Correlation,
                    },
                },
            }, cancellationToken);

            // marked, not deleted — the outbox doubles as the audit trail
            await using var mark = new NpgsqlCommand(
                "UPDATE messaging.outbox SET dispatchedat = now() WHERE id = @id", connection);
            mark.Parameters.AddWithValue("id", row.Id);
            await mark.ExecuteNonQueryAsync(cancellationToken);
        }

        if (rows.Count > 0)
        {
            logger.LogInformation("Dispatched {Count} outbox rows for {Org}/{Workspace}",
                rows.Count, org, workspace);
        }
        return rows.Count;
    }
}
