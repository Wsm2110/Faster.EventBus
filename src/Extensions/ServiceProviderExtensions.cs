using Faster.EventBus.Contracts;
using Faster.EventBus.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Faster.EventBus.Extensions;

/// <summary>
/// Provides extension methods for registering the EventBus infrastructure,
/// including automatic discovery of command handlers, event handlers, and pipeline behaviors.
/// </summary>

public static class ServiceProviderExtensions
{

    /// <summary>
    /// Registers the <see cref="EventDispatcher"/> and scans assemblies for handlers implementing:
    /// <list type="bullet">
    ///   <item><description><see cref="ICommandHandler{TCommand,TResponse}"/> for command processing</description></item>
    ///   <item><description><see cref="IEventHandler{TEvent}"/> for event subscription</description></item>
    ///   <item><description><see cref="ICommandPipelineBehavior{TCommand,TResponse}"/> for pipeline middleware</description></item>
    /// </list>
    /// Optionally applies <see cref="EventBusOptions"/> configuration and registers provided assemblies
    /// or falls back to the calling assembly if none are specified.
    /// </summary>
    /// <param name="services">The service collection to register dependencies into.</param>
    /// <param name="configure">Optional delegate to configure <see cref="EventBusOptions"/>.</param>
    /// <param name="assemblies">
    /// Assemblies to scan for handlers. If omitted, the calling assembly is used.
    /// </param>
    /// <returns>The original <see cref="IServiceCollection"/> for fluent chaining.</returns>

    public static IServiceCollection AddEventBus(
        this IServiceCollection services,
        Action<EventBusOptions>? configure = null,
        params Assembly[] assemblies)
    {
        // Build option instance to know AutoScan before DI container exists
        var options = new EventBusOptions();

        configure?.Invoke(options);

        // Persist config to DI
        services.Configure(configure ?? (_ => { }));

        // Scan calling assembly if none provided
        if (assemblies == null || assemblies.Length == 0)
            assemblies = new[] { Assembly.GetCallingAssembly() };



        // Always register dispatcher
        services.AddSingleton<EventDispatcher>();

        // Only auto-register handlers if AutoScan == true
        if (options.AutoScan)
        {
            var commandHandlerType = typeof(ICommandHandler<,>);
            var eventHandlerType = typeof(IEventHandler<>);
            var behaviorType = typeof(ICommandPipelineBehavior<,>);

            foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                foreach (var iface in type.GetInterfaces())
                {
                    if (!iface.IsGenericType)
                        continue;

                    var genericDef = iface.GetGenericTypeDefinition();

                    if (genericDef == commandHandlerType ||
                        genericDef == eventHandlerType ||
                        genericDef == behaviorType)

                    {
                        services.AddSingleton(iface, type);
                    }
                }
            }
        }

        return services;
    }
}