using Faster.EventBus.Contracts;
using Faster.EventBus.Core;
using Faster.EventBus.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Faster.EventBus.Tests;

public class EventDispatcherAsyncCommandHandlerTests
{
    [Fact]
    public async Task SendAsyncCommand_ShouldReturnGenericSuccessResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus(opt => opt.AutoScan = false);
        services.AddTransient<ICommandHandler<GetUserNameCommand, Result<string>>, GetUserNameCommandHandler>();

        //leftovers  from previous testcases
        services.AddSingleton<IList<string>, List<string>>();

        var provider = services.BuildServiceProvider();

        var bus = provider.GetRequiredService<EventDispatcher>();

        bus.RegisterCommandHandler<GetUserNameCommandHandler>();

        // Act
        var result = await bus.Send(new GetUserNameCommand(5));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("User-5", result.Value);
    }

    [Fact]
    public async Task SendAsyncCommand_WithFailure_ShouldPropagateError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus(opt => opt.AutoScan = true);

        services.AddSingleton<IList<string>, List<string>>();
        services.AddSingleton<MyDependency>();

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<EventDispatcher>();

        // Act
        var result = await bus.Send(new GetUserNameCommand(-1));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid ID", result.Error);
    }

    [Fact]
    public async Task SendAsyncCommand_ShouldSupportMatch()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus(opt => opt.AutoScan = true);

        // Auto scan also scanns handlers from other tests, thus this leftover 
        services.AddSingleton<IList<string>, List<string>>();
        services.AddSingleton<MyDependency>();

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<EventDispatcher>();


        string? matchOutput = null;

        // Act
        var result = await bus.Send(new GetUserNameCommand(10));

        result.Match(
           success: v => matchOutput = v,
           failure: err => matchOutput = $"Error: {err}"
        );

        // Assert
        Assert.Equal("User-10", matchOutput);
    }
}

// Example command with generic result return support
public record GetUserNameCommand(int Id) : ICommand<Result<string>>;

// Example async command handler
public class GetUserNameCommandHandler : ICommandHandler<GetUserNameCommand, Result<string>>
{
    public async ValueTask<Result<string>> Handle(GetUserNameCommand cmd, CancellationToken ct)
    {
        await Task.Delay(10, ct); // Simulating I/O work

        if (cmd.Id < 0)
            return Result<string>.Failure("Invalid ID");

        return Result<string>.Success($"User-{cmd.Id}");
    }
}