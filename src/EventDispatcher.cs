using Faster.EventBus.Contracts;
using Faster.EventBus.Core;
using Faster.EventBus.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Faster.EventBus;

/// <summary>
/// The main in-process event bus used to send commands and publish events.
/// <br/>
/// It supports:
/// <list type="bullet">
/// <item><description>Synchronous commands with a return type using <see cref="ICommand{TResponse}"/></description></item>
/// <item><description>Asynchronous event publishing (fan-out) using <see cref="IEvent"/></description></item>
/// </list>
/// <br/>
/// <b>How to send a command:</b>
/// <code>
/// var result = await bus.Send(new GetUserNameCommand(5));
/// Console.WriteLine(result.Value); // "John"
/// </code>
/// <br/>
/// <b>How to publish and subscribe to events:</b>
/// <code>
/// bus.SubscribeEvent&lt;UserCreatedEvent&gt;();
/// await bus.PublishEvent(new UserCreatedEvent(userId: 99));
/// </code>
/// <br/>
/// <b>How handlers are discovered:</b>
/// <code>
/// services.AddTransient&lt;ICommandHandler&lt;GetUserNameCommand, Result&lt;string&gt;&gt;, GetUserNameCommandHandler&gt;();
/// services.AddTransient&lt;IEventHandler&lt;UserCreatedEvent&gt;, UserCreatedEventHandler&gt;();
/// var provider = services.BuildServiceProvider();
/// var bus = provider.GetRequiredService&lt;EventDispatcher&gt;();
/// // handlers are automatically registered on startup
/// </code>
/// </summary>
public sealed class EventDispatcher : IEventDispatcher
{
    /// <summary>
    /// DI provider used to resolve command handlers and event handlers.
    /// </summary>
    private readonly IServiceProvider _provider;

    /// <summary>
    /// Cache of compiled delegates for command handler pipelines.
    /// Key is command type, and the value is a compiled fast delegate.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Delegate> _invokers = new();

    /// <summary>
    /// Creates a new EventDispatcher instance.
    /// Automatically scans all assemblies and registers all command handlers.
    /// </summary>
    /// <param name="provider">The DI service provider.</param>
    public EventDispatcher(IServiceProvider provider, IOptions<EventBusOptions> options)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));

        if (options.Value.AutoScan)
        {
            AutoRegisterAllHandlers();
            AutoScanEventHandlers();
        }
    }

    /// <summary>
    /// Sends a command and returns a response value asynchronously.
    /// <br/>
    /// Example:
    /// <code>
    /// var result = await bus.Send(new GetUserNameCommand(5));
    /// Console.WriteLine(result.Value); // "John"
    /// </code>
    /// </summary>
    public async ValueTask<TResponse> Send<TResponse>(
        ICommand<TResponse> command,
        CancellationToken ct = default)
    {
        var commandType = command.GetType();

        if (!_invokers.TryGetValue(commandType, out var del))
        {
            throw new InvalidOperationException($"Command handler missing: {commandType.Name}");
        }

        var invoker = (Func<ICommand<TResponse>, CancellationToken, ValueTask<TResponse>>)del;
        return await invoker(command, ct);
    }

    /// <summary>
    /// Subscribes all event handlers registered in DI for the specific event type.
    /// Run once at startup.
    /// <br/>
    /// Example:
    /// <code>
    /// bus.SubscribeEvent&lt;UserCreatedEvent&gt;();
    /// </code>
    /// </summary>
    public void Subscribe<TEvent>() where TEvent : IEvent
    {
        var handlers = _provider.GetServices<IEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            EventRoute<TEvent>.Subscribe(handler.Handle);
        }
    }

    /// <summary>
    /// Publishes an event to all subscribers.
    /// <br/>
    /// Example:
    /// <code>
    /// await bus.PublishEvent(new UserCreatedEvent(7));
    /// </code>
    /// </summary>
    public void Publish<TEvent>(TEvent evt, CancellationToken ct = default)
        where TEvent : IEvent
    {
        EventRoute<TEvent>.Publish(evt, ct);
    }

    /// <summary>
    /// Registers a command handler manually.
    /// Normally not required unless explicitly controlling registration.
    /// <br/>
    /// Example:
    /// <code>
    /// bus.RegisterCommandHandler&lt;GetUserNameCommandHandler&gt;();
    /// var r = await bus.Send(new GetUserNameCommand(7));
    /// </code>
    /// </summary>
    public void RegisterCommandHandler<THandler>()
    {
        var handlerType = typeof(THandler);

        var iface = handlerType.GetInterfaces()
            .First(i => i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));

        var commandType = iface.GetGenericArguments()[0];
        var responseType = iface.GetGenericArguments()[1];

        BuildPipeline(commandType, responseType);
    }

    /// <summary>
    /// Builds and caches a command execution pipeline delegate.
    /// Called once per command type.
    /// </summary>
    private void BuildPipeline(Type commandType, Type responseType)
    {
        var handlerInterface = typeof(ICommandHandler<,>).MakeGenericType(commandType, responseType);
        var handler = _provider.GetRequiredService(handlerInterface);

        var behaviorInterface = typeof(ICommandPipelineBehavior<,>).MakeGenericType(commandType, responseType);
        var behaviors = _provider.GetServices(behaviorInterface).DistinctBy(i => i!.GetType().Name).ToArray();

        var invoker = PipelineFactory.Build(commandType, handler, behaviors!, responseType);
        _invokers[commandType] = invoker;
    }

    /// <summary>
    /// Automatically discovers all ICommandHandler types in loaded assemblies
    /// and compiles & caches fast delegates for them.
    /// This means Send() uses zero reflection or allocations.
    /// </summary>
    private void AutoRegisterAllHandlers()
    {
        var ifaceType = typeof(ICommandHandler<,>);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var handlers = assemblies.SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract &&
                        t.GetInterfaces().Any(i => i.IsGenericType &&
                                                   i.GetGenericTypeDefinition() == ifaceType));

        foreach (var h in handlers)
        {
            var iface = h.GetInterfaces().First(i => i.IsGenericType &&
                                                     i.GetGenericTypeDefinition() == ifaceType);

            BuildPipeline(iface.GetGenericArguments()[0], iface.GetGenericArguments()[1]);
        }
    }

    private void AutoScanEventHandlers()
    {
        var eventHandlerInterface = typeof(IEventHandler<>);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var handlers = assemblies.SelectMany(a => a.GetTypes())
                                                   .Where(t => t.IsClass && !t.IsAbstract &&
                                                          t.GetInterfaces().Any(i => i.IsGenericType &&
                                                          i.GetGenericTypeDefinition() == eventHandlerInterface));

        HashSet<Type> seen = new HashSet<Type>();

        foreach (var handler in handlers)
        {
            var iface = handler.GetInterfaces().First(i => i.IsGenericType &&
                                                     i.GetGenericTypeDefinition() == eventHandlerInterface);
            var eventType = iface.GetGenericArguments()[0];
            if (!seen.Add(eventType))
            {
                continue;
            }

            var subscribeMethod = typeof(EventDispatcher)
                .GetMethod(nameof(Subscribe))!
                .MakeGenericMethod(eventType);

            subscribeMethod.Invoke(this, null);
        }
    }
}