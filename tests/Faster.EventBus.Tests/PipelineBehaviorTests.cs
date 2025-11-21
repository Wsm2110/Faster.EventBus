using Faster.EventBus.Contracts;
using Faster.EventBus.Extensions;
using global::Faster.EventBus.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Faster.EventBus.Tests;

public class PipelineBehaviorTests
{
    [Fact]
    public async Task PipelineBehaviors_ShouldExecuteInCorrectOrderAutoScann()
    {
        var services = new ServiceCollection();
        services.AddEventBus(configure: (options) =>
        {
            options.AutoScan = true;
        });

        // Auto scan also scanns handlers from other tests, thus since we use autoscan... we have to register these types manually
        services.AddSingleton<IList<string>, List<string>>();
        services.AddSingleton<MyDependency>();

        services.AddSingleton<EventDispatcher>();

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IEventDispatcher>()
            .Initialize();

        await bus.Send(new TestPipeline());

        var execution = provider.GetRequiredService<IList<string>>();

        Assert.Equal(new[] { "first-before", "second-before", "handler", "second-after", "first-after" }, execution);
    }


    [Fact]
    public async Task PipelineBehaviors_ShouldExecuteInCorrectOrder()
    {
        var services = new ServiceCollection();
        services.AddEventBus(configure: (options) =>
        {
            options.AutoScan = false;
        });

        services.AddSingleton<IList<string>, List<string>>();
        services.AddSingleton<ICommandHandler<TestPipeline, Result>, PipelineTestHandler>();
        services.AddSingleton<ICommandPipelineBehavior<TestPipeline, Result>, FirstBehavior>();
        services.AddSingleton<ICommandPipelineBehavior<TestPipeline, Result>, SecondBehavior>();

        services.AddSingleton<EventDispatcher>();

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IEventDispatcher>().Initialize();

        // Required since auto register is off
        bus.RegisterCommandHandler<PipelineTestHandler>();

        await bus.Send(new TestPipeline());

        var execution = provider.GetRequiredService<IList<string>>();

        Assert.Equal(new[] { "first-before", "second-before", "handler", "second-after", "first-after" }, execution);
    }
}

public record TestPipeline() : ICommand<Result>;

public class PipelineTestHandler(IList<string> exec) : ICommandHandler<TestPipeline, Result>
{
    public ValueTask<Result> Handle(TestPipeline command, CancellationToken ct)
    {
        exec.Add("handler");
        return ValueTask.FromResult(Result.Success());
    }
}

public class FirstBehavior(IList<string> exec) : ICommandPipelineBehavior<TestPipeline, Result>
{
    public async ValueTask<Result> Handle(TestPipeline cmd, CancellationToken ct, CommandHandlerDelegate<Result> next)
    {
        exec.Add("first-before");
        var result = await next();
        exec.Add("first-after");
        return result;
    }
}

public class SecondBehavior(IList<string> exec) : ICommandPipelineBehavior<TestPipeline, Result>
{

    public async ValueTask<Result> Handle(TestPipeline cmd, CancellationToken ct, CommandHandlerDelegate<Result> next)
    {
        exec.Add("second-before");
        var result = await next();
        exec.Add("second-after");
        return result;
    }
}