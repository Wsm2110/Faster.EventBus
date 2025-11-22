using Faster.EventBus.Contracts;
using Faster.EventBus.Extensions;
using Faster.EventBus.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Faster.EventBus.Tests;

public record InjectedCommand(int Value) : ICommand<Result<int>>;

public sealed class MyDependency
{
    public int Multiply(int x) => x * 10;
}

public sealed class InjectedCommandHandler :
    ICommandHandler<InjectedCommand, Result<int>>
{
    private readonly MyDependency _dep;

    public InjectedCommandHandler(MyDependency dep)
    {
        _dep = dep ?? throw new ArgumentNullException(nameof(dep));
    }

    public ValueTask<Result<int>> Handle(InjectedCommand cmd, CancellationToken ct)
    {
        var computed = _dep.Multiply(cmd.Value);
        return ValueTask.FromResult(Result<int>.Success(computed));
    }
}

public class DependencyInjectionTests
{
    [Fact]
    public async Task CommandHandler_ShouldUseInjectedDependency()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus();

        services.AddSingleton<MyDependency>();
        services.AddSingleton<ICommandHandler<InjectedCommand, Result<int>>, InjectedCommandHandler>();

        services.AddSingleton<EventBus>();
        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IEventBus>();

        // Act
        var result = await bus.Send(new InjectedCommand(5));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value);
    }

    [Fact]
    public async Task CommandHandler_ShouldUseInjectedDependency_autoscan()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus();

        services.AddSingleton<MyDependency>();
        services.AddSingleton<IList<string>, List<string>>();
        services.AddSingleton<EventBus>();

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IEventBus>();

        // Act
        var result = await bus.Send(new InjectedCommand(5));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value);
    }
}

