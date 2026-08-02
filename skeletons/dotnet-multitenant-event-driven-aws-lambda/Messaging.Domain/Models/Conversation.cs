namespace Messaging.Domain.Models;

public static class ConversationStatus
{
    public const string Open = "open";
    public const string Closed = "closed";
}

/// <summary>Rich entity (adrs/dotnet/rich-domain-entities.md).</summary>
public class Conversation
{
    private Conversation() { } // EF Core

    public Guid Id { get; private set; }
    public string ContactName { get; private set; } = string.Empty;
    public string Status { get; private set; } = ConversationStatus.Open;
    public DateTimeOffset CreatedAt { get; private set; }

    public static Conversation Create(string contactName)
    {
        if (string.IsNullOrWhiteSpace(contactName) || contactName.Length > 200)
        {
            throw new ArgumentException("Contact name must be 1-200 characters.", nameof(contactName));
        }

        return new Conversation
        {
            Id = Guid.CreateVersion7(),
            ContactName = contactName.Trim(),
            Status = ConversationStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
