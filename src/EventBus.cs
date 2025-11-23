using System.Runtime.CompilerServices;
using Faster.EventBus.Contracts;

namespace Faster.EventBus;

/// <summary>
/// <para>
/// The <see cref="EventBus"/> serves as a high-level unified API for all interaction
/// with the event-driven messaging system.
/// </para>
/// <para>
/// It provides a consolidated facade allowing clients to:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Send commands through the command pipeline</description>
/// </item>
/// <item>
/// <description>Run use case requests through the request/response pipeline</description>
/// </item>
/// <item>
/// <description>Publish events to all subscribed event handlers</description>
/// </item>
/// </list>
/// <para>
/// This class itself contains no business logic. Instead it delegates execution to:
/// </para>
/// <list type="bullet">
/// <item>
/// <description><see cref="ICommandDispatcher"/> — responsible for command execution</description>
/// </item>
/// <item>
/// <description><see cref="IUseCaseDispatcher"/> — responsible for use case invocation</description>
/// </item>
/// <item>
/// <description><see cref="IEventDispatcher"/> — responsible for event publishing</description>
/// </item>
/// </list>
/// <para>
/// The goal of this abstraction is to provide simplicity and reduce the need for consumers
/// to depend on multiple dispatching abstractions.
/// </para>
/// </summary>
public sealed class EventBus(
    ICommandDispatcher CommandDispatcher,
    IEventDispatcher EventDispatcher,
    IUseCaseDispatcher UseCaseDispatcher) : IEventBus
{
    /// <summary>
    /// Sends a command into the command pipeline, triggering the appropriate command handler
    /// that corresponds to the type of the <paramref name="command"/>.
    ///
    /// This is commonly used for state-changing operations in CQRS-based architecture.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type produced by the command handler.</typeparam>
    /// <param name="command">The command object containing execution context and input data.</param>
    /// <param name="ct">Optional <see cref="CancellationToken"/> for cooperative cancellation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResponse}"/> representing asynchronous command execution.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
    {
        // Delegate command execution to the command dispatcher abstraction.
        return CommandDispatcher.Send(command, ct);
    }

    /// <summary>
    /// Executes a use case that follows request/response semantics using the configured
    /// use case dispatcher. This supports high-performance processing where
    /// <typeparamref name="TResponse"/> represents the response produced by the operation.
    /// </summary>
    /// <typeparam name="TResponse">The response type returned by the use case.</typeparam>
    /// <param name="useCase">The use case request to execute.</param>
    /// <param name="ct">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResponse}"/> representing execution of the use case.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> Run<TResponse>(IUseCase<TResponse> useCase, CancellationToken ct = default)
        => UseCaseDispatcher.Execute(useCase, ct);

    /// <summary>
    /// Publishes an event to all handlers that are subscribed to the type of <typeparamref name="TEvent"/>.
    /// Handlers execute independently and may represent side effects or asynchronous processing.
    /// </summary>
    /// <typeparam name="TEvent">The event type being published.</typeparam>
    /// <param name="event">The event instance to broadcast.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Publish<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IEvent
    {
        // Delegate event distribution to the event dispatcher.
        EventDispatcher.Publish(@event, ct);
    }
}
