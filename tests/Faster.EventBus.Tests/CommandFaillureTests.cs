using Faster.EventBus.Contracts;
using Faster.EventBus.Core;
using Faster.EventBus.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Faster.EventBus.Tests;

public class CommandFailureTests
{
    [Fact]
    public async Task SendCommand_WhenFails_ShouldReturnFailure()
    {
        var services = new ServiceCollection();

        services.AddEventBus(configure: (options) =>
        {
            options.AutoScan = false;
        });

        services.AddSingleton<ICommandHandler<FailCommand, Result>, FailCommandHandler>();
        services.AddSingleton<EventDispatcher>();

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<EventDispatcher>();

        bus.RegisterCommandHandler<FailCommandHandler>();

        var result = await bus.Send(new FailCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal("Something went wrong", result.Error);
    }

    [Fact]
    public async Task SendCommand__WhenFails_ShouldReturnFailure_Part_Two()
    {
        var services = new ServiceCollection();

        services.AddEventBus(configure: (options) =>
        {
            options.AutoScan = true;
        });

        services.AddSingleton<EventDispatcher>();
        services.AddSingleton<IList<string>, List<string>>();
        services.AddSingleton<MyDependency>();

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<EventDispatcher>();

        var result = await bus.Send(new FailCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal("Something went wrong", result.Error);
    }

    [Fact]
    public async Task SendCommand__WhenFails_ShouldReturnFailure_Part_Three()
    {
        var services = new ServiceCollection();

        services.AddEventBus(configure: (options) =>
        {
            options.AutoScan = true;
        });

        services.AddSingleton<ICommandHandler<FailCommand, Result>, FailCommandHandler>();
        services.AddSingleton<EventDispatcher>();

        //leftovers...
        services.AddSingleton<IList<string>, List<string>>();
        services.AddSingleton<MyDependency>();

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<EventDispatcher>();

        var result = await bus.Send(new FailCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal("Something went wrong", result.Error);
    }
}

public record FailCommand() : ICommand<Result>;

public class FailCommandHandler : ICommandHandler<FailCommand, Result>
{
    public ValueTask<Result> Handle(FailCommand command, CancellationToken ct)
        => ValueTask.FromResult(Result.Failure("Something went wrong"));
}