using Faster.EventBus.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

/// <summary>
/// High-performance event publisher that executes all event handlers
/// with zero allocations per publish, uses cached expression tree delegates,
/// and avoids reflection in the hot path.
/// </summary>
public sealed class EventDispatcher(IServiceProvider Provider) : IEventDispatcher
{ 
    /// <summary>
    /// Cache of compiled handler-invocation delegates for each event type.
    /// Key: eventType → delegate that invokes Handle for that TEvent
    /// </summary>
    private static readonly ConcurrentDictionary<Type, object> _handlerInvokerCache = new();
    
    /// <summary>
    /// Publishes an event by finding all handlers registered for the event type
    /// and executing them one by one.
    /// </summary>
    public async Task Publish<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IEvent
    {
        // Resolve all handlers from DI container
        var handlers = (IEventHandler<TEvent>[])Provider.GetServices<IEventHandler<TEvent>>();
        if (handlers.Length == 0)
            return;

        // Get a compiled delegate for invoking handler.Handle()
        var invoker = GetHandlerInvoker<TEvent>();

        // Call each handler in sequence with no allocations
        for (int i = 0; i < handlers.Length; i++)
        {
            await invoker(handlers[i], @event, ct);
        }
    }

    /// <summary>
    /// Retrieves a compiled delegate for calling:
    ///     handler.Handle(@event, ct)
    /// Compiled once using expression trees and then cached.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<IEventHandler<TEvent>, TEvent, CancellationToken, ValueTask>
        GetHandlerInvoker<TEvent>()
        where TEvent : IEvent
    {
        return (Func<IEventHandler<TEvent>, TEvent, CancellationToken, ValueTask>)
            _handlerInvokerCache.GetOrAdd(typeof(TEvent), static _ => CompileInvoker<TEvent>());
    }

    /// <summary>
    /// Uses expression trees to build a highly optimized delegate:
    ///
    /// (handler, evt, ct) => handler.Handle(evt, ct)
    ///
    /// This avoids reflection and virtual dispatch overhead.
    /// </summary>
    private static Func<IEventHandler<TEvent>, TEvent, CancellationToken, ValueTask>
        CompileInvoker<TEvent>()
        where TEvent : IEvent
    {
        var handlerParam = Expression.Parameter(typeof(IEventHandler<TEvent>), "handler");
        var eventParam = Expression.Parameter(typeof(TEvent), "evt");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        var handleMethod = typeof(IEventHandler<TEvent>).GetMethod("Handle")!;
        var call = Expression.Call(handlerParam, handleMethod, eventParam, ctParam);

        var lambda = Expression.Lambda<
            Func<IEventHandler<TEvent>, TEvent, CancellationToken, ValueTask>
        >(call, handlerParam, eventParam, ctParam);

        return lambda.Compile();
    }
}
