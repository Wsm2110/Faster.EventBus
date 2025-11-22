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
    /// Registers the <see cref="EventBus"/> and scans assemblies for handlers implementing:
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
        params Assembly[] assemblies)
    {
        // Scan calling assembly if none provided
        if (assemblies == null || assemblies.Length == 0)
            assemblies = new[] { Assembly.GetCallingAssembly() };

        // Core Services
        services.AddSingleton<IPipelineFactory, PipelineFactory>();

        // Main API - Registered as implemented interfaces;
        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
        services.AddSingleton<IEventDispatcher, EventDispatcher>();
        services.AddSingleton<IUseCaseDispatcher, UseCaseDispatcher>();


        var commandHandlerType = typeof(ICommandHandler<,>);
        var eventHandlerType = typeof(IEventHandler<>);
        var behaviorType = typeof(IPipelineBehavior<,>);
        var useCaseType = typeof(IUseCaseHandler<,>);

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
                    genericDef == behaviorType ||
                    genericDef == useCaseType)
                {
                    services.AddSingleton(iface, type);
                }
            }
        }

        return services;
    }
}