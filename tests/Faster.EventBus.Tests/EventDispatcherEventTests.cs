using Faster.EventBus.Contracts;
using Faster.EventBus.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        var bus = provider.GetRequiredService<EventDispatcher>();

        bus.SubscribeEvent<NumberAddedEvent>();

        bus.PublishEvent(new NumberAddedEvent(99));

        Assert.Equal(99, handler1.LastValue);
        Assert.Equal(99, handler2.LastValue);
    }

    [Fact]
    public async Task SubscribeAndPublish_EventIsHandledByRegisteredHandler()
    {
        // Arrange
        UserCreatedEventHandler.ProcessedUserIds.Clear(); // Clear handler results

        var dispatcher = CreateDispatcher();
        var userIdToPublish = 42;
        var evt = new UserCreatedEvent(userIdToPublish);

        // Act - 1: Subscribe
        // This finds the IEventHandler<UserCreatedEvent> in DI (UserCreatedEventHandler)
        // and registers its Handle method with EventRoute<UserCreatedEvent>.
        dispatcher.SubscribeEvent<UserCreatedEvent>();

        // Assert - 1: Check subscription (implicitly, we can't directly check the private list)
        // We rely on Publish to confirm subscription was successful.

        // Act - 2: Publish
        dispatcher.PublishEvent(evt);

        // Assert - 2: Verify the handler was executed and processed the event
        Assert.Single(UserCreatedEventHandler.ProcessedUserIds);
        Assert.Equal(userIdToPublish, UserCreatedEventHandler.ProcessedUserIds[0]);
    }

    [Fact]
    public async Task Publish_NoSubscription_EventIsNotHandled()
    {
        // Arrange
        UserCreatedEventHandler.ProcessedUserIds.Clear();

        var dispatcher = CreateDispatcher();
        var evt = new UserCreatedEvent(99);

        // Act: Publish without calling SubscribeEvent first
        dispatcher.PublishEvent(evt);

        // Assert: The handler's static list should be empty
        Assert.Empty(UserCreatedEventHandler.ProcessedUserIds);
    }

    private EventDispatcher CreateDispatcher()
    {
        // 1. Set up DI container
        var services = new ServiceCollection();

        // Register the handler
        services.AddTransient<IEventHandler<UserCreatedEvent>, UserCreatedEventHandler>();

        // Register IOptions<EventBusOptions> - important for the constructor
        var options = Options.Create(new EventBusOptions { AutoScan = false });
        services.AddSingleton(options);

        var provider = services.BuildServiceProvider();

        // 2. Create the dispatcher instance
        return new EventDispatcher(provider, options);
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