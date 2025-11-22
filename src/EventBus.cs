using System.Runtime.CompilerServices;
using Faster.EventBus.Contracts;

namespace Faster.EventBus;

/// <summary>
/// The EventDispatcher is a high-level facade that provides a unified entry point
/// for interacting with the event bus system. It allows users to:
///  - Send commands through the command pipeline
///  - Publish events to all subscribed event handlers
///
/// This class does not contain any command or event handling logic itself.
/// Instead, it delegates responsibility to:
///  - <see cref="ICommandDispatcher"/> for executing commands
///  - <see cref="IEventDispatcher"/> for publishing events
///
/// The purpose of this abstraction is to provide convenience and decouple consumers
/// from the internal implementation details of pipelines and dispatching.
/// </summary>
public sealed class EventBus(
    ICommandDispatcher CommandDispatcher, 
    IEventDispatcher EventDispatcher,
    IUseCaseDispatcher UseCaseDispatcher) : IEventBus
{
    /// <summary>
    /// Sends a command through the command pipeline and returns a response.
    /// The appropriate command handler will be executed based on the command type.
    /// </summary>
    /// <typeparam name="TResponse">The response type expected from the command.</typeparam>
    /// <param name="command">The command object to be processed.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A <see cref="ValueTask{TResponse}"/> representing the asynchronous execution.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
    {
        // Dispatch command execution via the command sender abstraction.
        return CommandDispatcher.Send(command, ct);
    }

    public ValueTask<TResponse> Run<TRequest, TResponse>(
      TRequest request,
      CancellationToken ct = default)
      => UseCaseDispatcher.Execute<TRequest, TResponse>(request, ct);

    /// <summary>
    /// Publishes an event to all registered event handlers for its type.
    /// Multiple handlers may process a single event.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to publish.</typeparam>
    /// <param name="event">The event instance.</param>
    /// <param name="ct">Optional cancellation token.</param>
    public void Publish<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IEvent
    {
        // Dispatch the event to the event publisher, which will fan-out to all handlers.
        EventDispatcher.Publish(@event, ct);
    }
}