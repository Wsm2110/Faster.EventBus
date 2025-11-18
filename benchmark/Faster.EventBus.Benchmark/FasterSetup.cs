using Faster.EventBus;
using Faster.EventBus.Contracts;
using Faster.EventBus.Core;
using Faster.EventBus.Extensions;
using Microsoft.Extensions.DependencyInjection;

public static class FasterSetup
{
    public static EventDispatcher Build()
    {
        var services = new ServiceCollection();
        services.AddEventBus(o => o.AutoScan = true);

        services.AddSingleton<ICommandHandler<TestResultCommand, Result<string>>, TestResultHandler>();
        services.AddSingleton<ICommandPipelineBehavior<TestResultCommand, Result<string>>, ResultBehavior>();
        services.AddSingleton<EventDispatcher>();

        return services.BuildServiceProvider().GetRequiredService<EventDispatcher>();
    }
}
