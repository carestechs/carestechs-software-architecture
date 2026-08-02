using System.Text.Json;
using Messaging.Application.Internal;
using Messaging.Domain.Models;

namespace Messaging.Application;

/// <summary>Facade implementation — owns the per-tenant UoW lifecycle internally
/// (adrs/dotnet/module-facade.md).</summary>
public sealed class MessagingModuleApi : IMessagingModuleApi
{
    public const string MessageAppendedQueue = "message-appended-queue";

    private readonly IMessagingUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IMessageStore _messages;

    internal MessagingModuleApi(IMessagingUnitOfWorkFactory unitOfWorkFactory, IMessageStore messages)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _messages = messages;
    }

    public async Task<Guid> StartConversationAsync(
        string orgId, string workspaceId, string contactName, CancellationToken cancellationToken)
    {
        await using var unitOfWork = _unitOfWorkFactory.OpenForTenant(orgId, workspaceId);
        var conversation = Conversation.Create(contactName);
        unitOfWork.AddConversation(conversation);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return conversation.Id;
    }

    public async Task<ConversationSnapshot?> GetConversationAsync(
        string orgId, string workspaceId, Guid conversationId, CancellationToken cancellationToken)
    {
        await using var unitOfWork = _unitOfWorkFactory.OpenForTenant(orgId, workspaceId);
        var conversation = await unitOfWork.GetConversationAsync(conversationId, cancellationToken);
        return conversation is null
            ? null
            : new ConversationSnapshot(
                conversation.Id, conversation.ContactName, conversation.Status, conversation.CreatedAt);
    }

    public async Task<bool> AppendMessageAsync(
        string orgId, string workspaceId, Guid conversationId, string sender, string text,
        string correlationId, CancellationToken cancellationToken)
    {
        await using var unitOfWork = _unitOfWorkFactory.OpenForTenant(orgId, workspaceId);
        var conversation = await unitOfWork.GetConversationAsync(conversationId, cancellationToken);
        if (conversation is null)
        {
            return false;
        }

        var messageId = Guid.CreateVersion7();
        var sentAt = DateTimeOffset.UtcNow;

        // Hot path first (DynamoDB is not transactional with PG — the outbox row
        // below announces the fact, and consumers are idempotent; see
        // adrs/database/dynamodb-hot-path.md + adrs/database/transactional-outbox.md)
        await _messages.PutAsync(
            orgId, workspaceId, conversationId, messageId, sender, text, sentAt, cancellationToken);

        unitOfWork.AddOutboxRow(
            MessageAppendedQueue,
            JsonSerializer.Serialize(new { conversationId, messageId }),
            correlationId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<MessageSnapshot>> ListMessagesAsync(
        string orgId, string workspaceId, Guid conversationId, CancellationToken cancellationToken)
    {
        var items = await _messages.ListAsync(orgId, workspaceId, conversationId, cancellationToken);
        return items.Select(m => new MessageSnapshot(m.Id, m.Sender, m.Text, m.SentAt)).ToList();
    }
}
