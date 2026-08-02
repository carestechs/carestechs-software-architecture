using Messaging.Domain.Models;

namespace Messaging.Application.Internal;

/// <summary>Per-purpose contracts stay INTERNAL — consumers see only the facade
/// (adrs/dotnet/module-facade.md).</summary>
internal interface IMessagingUnitOfWork : IAsyncDisposable
{
    Task<Conversation?> GetConversationAsync(Guid id, CancellationToken cancellationToken);
    void AddConversation(Conversation conversation);
    void AddOutboxRow(string queueName, string payload, string correlationId);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

internal interface IMessagingUnitOfWorkFactory
{
    /// <summary>Canonical per-tenant unit-of-work entry
    /// (adrs/database/database-per-tenant.md).</summary>
    IMessagingUnitOfWork OpenForTenant(string organizationId, string workspaceId);
}

internal interface IMessageStore
{
    Task PutAsync(string orgId, string workspaceId, Guid conversationId,
        Guid messageId, string sender, string text, DateTimeOffset sentAt,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<(Guid Id, string Sender, string Text, DateTimeOffset SentAt)>> ListAsync(
        string orgId, string workspaceId, Guid conversationId, CancellationToken cancellationToken);
}
