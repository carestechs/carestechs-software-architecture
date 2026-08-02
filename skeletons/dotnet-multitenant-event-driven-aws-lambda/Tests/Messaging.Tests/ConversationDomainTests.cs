using Messaging.Domain.Models;
using Xunit;

namespace Messaging.Tests;

public class ConversationDomainTests
{
    [Fact]
    public void Create_EnforcesInvariants()
    {
        var conversation = Conversation.Create("  Ada Lovelace ");
        Assert.Equal("Ada Lovelace", conversation.ContactName);
        Assert.Equal(ConversationStatus.Open, conversation.Status);
    }

    [Fact]
    public void Create_RejectsBlankContact()
    {
        Assert.ThrowsAny<ArgumentException>(() => Conversation.Create("  "));
    }
}
