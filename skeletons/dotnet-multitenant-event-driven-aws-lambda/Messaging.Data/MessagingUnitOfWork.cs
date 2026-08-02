using Common.Lib.Tenancy;
using Messaging.Application.Internal;
using Messaging.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Messaging.Data;

internal sealed class MessagingUnitOfWork(MessagingDbContext context) : IMessagingUnitOfWork
{
    public Task<Conversation?> GetConversationAsync(Guid id, CancellationToken cancellationToken) =>
        context.Conversations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void AddConversation(Conversation conversation) =>
        context.Conversations.Add(conversation);

    public void AddOutboxRow(string queueName, string payload, string correlationId) =>
        context.Outbox.Add(new OutboxRow
        {
            QueueName = queueName,
            Payload = payload,
            CorrelationId = correlationId,
        });

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync() => context.DisposeAsync();
}

internal sealed class MessagingUnitOfWorkFactory(
    string host, int port, string user, string password) : IMessagingUnitOfWorkFactory
{
    public IMessagingUnitOfWork OpenForTenant(string organizationId, string workspaceId)
    {
        // tenant identifiers are validated here, at the boundary — never assumed
        if (!OrgId.TryParse(organizationId, out var org))
        {
            throw new ArgumentException("Invalid organization id.", nameof(organizationId));
        }
        if (!WorkspaceId.TryParse(workspaceId, out var workspace))
        {
            throw new ArgumentException("Invalid workspace id.", nameof(workspaceId));
        }

        var options = new DbContextOptionsBuilder<MessagingDbContext>()
            .UseNpgsql(TenantDbConnectionBuilder.BuildConnectionString(
                host, port, user, password, org, workspace))
            .Options;
        return new MessagingUnitOfWork(new MessagingDbContext(options));
    }
}
