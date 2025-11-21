using Faster.EventBus.Contracts;
using Faster.EventBus.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Faster.EventBus.Tests;

public class EventDispatcherEventTests
{
    [Fact]
    public async Task PublishEvent_ShouldInvokeAllHandlers()
    {
        var handler1 = new NumberAddedHandler();
        var handler2 = new NumberAddedHandler();

        var services = new ServiceCollection();
        services.AddEventBus(configure: (options) =>
        {
            options.AutoScan = false;
        });

        services.AddSingleton<IEventHandler<NumberAddedEvent>>(handler1);
        services.AddSingleton<IEventHandler<NumberAddedEvent>>(handler2);
        services.AddSingleton<EventDispatcher>();

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IEventDispatcher>().Initialize();

        bus.Subscribe<NumberAddedEvent>();

        bus.Publish(new NumberAddedEvent(99));

        await Task.Delay(1000); // Wait for async handlers to complete

        Assert.Equal(99, handler1.LastValue);
        Assert.Equal(99, handler2.LastValue);
    }

}
// 2. Event and Handler Implementations
public record UserCreatedEvent(int UserId) : IEvent;

public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    // Static list to verify handler was executed and received the correct data
    public static List<int> ProcessedUserIds = new();

    public ValueTask Handle(UserCreatedEvent evt, CancellationToken ct = default)
    {
        ProcessedUserIds.Add(evt.UserId);
        return ValueTask.CompletedTask;
    }
}

public record NumberAddedEvent(int Value) : IEvent;

public class NumberAddedHandler : IEventHandler<NumberAddedEvent>
{
    public int LastValue { get; private set; }

    public ValueTask Handle(NumberAddedEvent evt, CancellationToken ct)
    {
        LastValue = evt.Value;
        return ValueTask.CompletedTask;
    }
}