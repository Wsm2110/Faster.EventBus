using Faster.EventBus.Contracts;
using Faster.EventBus.Shared;
using Faster.EventBus.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Faster.EventBus.Tests;

public sealed class UseCaseTests
{
    [Fact]
    public async Task ExecuteUseCase_WhenFails_ShouldReturnFailure()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddEventBus(); // registers dispatcher + infrastructure

        services.AddSingleton<IUseCaseHandler<TestUseCaseRequest, Result>, TestUseCaseHandler>();
        services.AddSingleton<IEventBus, EventBus>();

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IEventBus>();

        // Act
        var result = await bus.Run<TestUseCaseRequest, Result>(new TestUseCaseRequest("X"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Use case failed", result.Error);
    }
}

public record TestUseCaseRequest(string Value);

public sealed class TestUseCaseHandler :
    IUseCaseHandler<TestUseCaseRequest, Result>
{
    public ValueTask<Result> Handle(TestUseCaseRequest request, CancellationToken ct = default)
        => ValueTask.FromResult(Result.Failure("Use case failed"));
}


