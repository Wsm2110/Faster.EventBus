using Faster.EventBus;
using Faster.EventBus.Contracts;
using Faster.EventBus.Extensions;
using Faster.EventBus.Shared;
using Microsoft.Extensions.DependencyInjection;

public static class FasterSetup
{
    public static Faster.EventBus.EventBus Build()
    {
        var services = new ServiceCollection();
        services.AddEventBus();

        services.AddSingleton<ICommandHandler<TestResultCommand, Result<string>>, TestResultHandler>();
       // services.AddSingleton<ICommandPipelineBehavior<TestResultCommand, Result<string>>, ResultBehavior>();
        services.AddSingleton<Faster.EventBus.EventBus>();

        return services.BuildServiceProvider().GetRequiredService<Faster.EventBus.EventBus>();
    }
}
