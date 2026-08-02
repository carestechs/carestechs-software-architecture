using Common.Lib.Contracts;
using Orders.Application.Commands;
using Orders.Application.Commands.Handlers;
using Orders.Application.Contracts;
using Orders.Application.Events;
using Orders.Domain.Models;
using Xunit;

namespace Orders.Tests;

public class PlaceOrderHandlerTests
{
    private sealed class FakeRepository : IOrderRepository
    {
        public List<Order> Items { get; } = [];
        public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Items.FirstOrDefault(o => o.Id == id));
        public void Add(Order order) => Items.Add(order);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class RecordingBus : IEventBus
    {
        public List<object> Published { get; } = [];
        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct)
            where TEvent : IEvent
        {
            Published.Add(domainEvent!);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Handle_PersistsTheOrder_AndPublishesOrderPlaced()
    {
        var repository = new FakeRepository();
        var bus = new RecordingBus();
        var handler = new PlaceOrderCommandHandler(repository, new FakeUnitOfWork(), bus);

        var result = await handler.HandleAsync(
            new PlaceOrderCommand(Guid.CreateVersion7(), 3), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Single(repository.Items);
        var published = Assert.IsType<OrderPlacedEvent>(Assert.Single(bus.Published));
        Assert.Equal(result.Value, published.OrderId);
    }

    [Fact]
    public async Task Handle_RejectsInvalidQuantity_WithoutPublishing()
    {
        var bus = new RecordingBus();
        var handler = new PlaceOrderCommandHandler(new FakeRepository(), new FakeUnitOfWork(), bus);

        var result = await handler.HandleAsync(
            new PlaceOrderCommand(Guid.CreateVersion7(), 0), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Empty(bus.Published);
    }
}
