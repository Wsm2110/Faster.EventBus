using Faster.EventBus.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Faster.EventBus.Extensions;

public static class ServiceProviderExtensions
{
    /// <summary>
    /// Extension to register EventBus and auto-discover handlers.
    /// </summary>
    public static IServiceCollection AddEventBus(
        this IServiceCollection services,
        Action<EventBusOptions>? configure = null,
        params Assembly[] assemblies)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<EventBusOptions>(_ => { });
        }

        if (assemblies == null || assemblies.Length == 0)
            assemblies = new[] { Assembly.GetCallingAssembly() };

        services.AddSingleton<EventDispatcher>();

        var commandHandlerType = typeof(ICommandHandler<,>);
        var eventHandlerType = typeof(IEventHandler<>);
        var behaviorType = typeof(ICommandPipelineBehavior<,>);
            
        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            foreach (var iface in type.GetInterfaces()) // <--- Only retrieve once
            {
                if (!iface.IsGenericType)
                    continue; // Optimization: Skip non-generic interfaces

                var genericDef = iface.GetGenericTypeDefinition();

                if (genericDef == commandHandlerType ||
                    genericDef == eventHandlerType ||
                    genericDef == behaviorType)
                {
                    // Register once the generic type is identified
                    services.AddTransient(iface, type);
                }
            }
        }

        return services;
    }
}