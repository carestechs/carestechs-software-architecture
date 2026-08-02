using Orders.Domain.Models;
using Xunit;

namespace Orders.Tests;

public class OrderDomainTests
{
    [Fact]
    public void Confirm_IsIdempotent()
    {
        var order = Order.Create(Guid.CreateVersion7(), 2);
        Assert.Equal(OrderStatus.Placed, order.Status);

        Assert.True(order.Confirm());
        var firstConfirmation = order.ConfirmedAt;
        Assert.NotNull(firstConfirmation);

        // the at-least-once pipeline may deliver twice — the second confirm is a no-op
        Assert.False(order.Confirm());
        Assert.Equal(firstConfirmation, order.ConfirmedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void Create_RejectsInvalidQuantity(int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Order.Create(Guid.CreateVersion7(), quantity));
    }
}
