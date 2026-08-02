namespace Messaging.Application;

/// <summary>The ONE public surface of this module (adrs/dotnet/module-facade.md):
/// snapshot records, primitive tenant identifiers, a CancellationToken on every
/// operation. Everything else in this module is internal.</summary>
public interface IMessagingModuleApi
{
    Task<Guid> StartConversationAsync(
        string orgId, string workspaceId, string contactName, CancellationToken cancellationToken);

    Task<ConversationSnapshot?> GetConversationAsync(
        string orgId, string workspaceId, Guid conversationId, CancellationToken cancellationToken);

    /// <summary>Appends to the DynamoDB hot path and records the fact in the
    /// tenant outbox (same PG transaction as the outbox row). Returns false when
    /// the conversation does not exist.</summary>
    Task<bool> AppendMessageAsync(
        string orgId, string workspaceId, Guid conversationId, string sender, string text,
        string correlationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MessageSnapshot>> ListMessagesAsync(
        string orgId, string workspaceId, Guid conversationId, CancellationToken cancellationToken);
}

public sealed record ConversationSnapshot(Guid Id, string ContactName, string Status, DateTimeOffset CreatedAt);

public sealed record MessageSnapshot(Guid Id, string Sender, string Text, DateTimeOffset SentAt);
