using Faster.EventBus.Core;
using System;
using System.Threading;

namespace Faster.EventBus.Contracts
{
    /// <summary>
    /// Represents a unified messaging interface that combines both
    /// command execution and event publishing into a single abstraction.
    ///
    /// This makes it possible for application code to interact with a single
    /// service for both actions (commands) and notifications (events),
    /// without needing to know how handlers and pipelines are implemented.
    ///
    /// <para>
    /// Commands:
    ///   - Represent an action that should be performed (e.g., CreateOrderCommand).
    ///   - Must have exactly one handler and return a result.
    /// </para>
    ///
    /// <para>
    /// Events:
    ///   - Represent something that already happened (e.g., OrderCreatedEvent).
    ///   - May have zero, one, or many handlers.
    ///   - Return no result.
    /// </para>
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Publishes an event to all handlers that subscribe to the event type.
        /// Each handler will receive and process the event independently.
        /// </summary>
        /// <typeparam name="TEvent">The event type being published.</typeparam>
        /// <param name="evt">The event instance containing event data.</param>
        /// <param name="ct">Optional cancellation token used for cooperative cancellation.</param>
        void Publish<TEvent>(TEvent evt, CancellationToken ct = default) where TEvent : IEvent;

        /// <summary>
        /// Sends a command and returns a response from the corresponding command handler.
        /// Unlike events, a command must have exactly one handler that returns a result.
        /// </summary>
        /// <typeparam name="TResponse">The expected return type from the command.</typeparam>
        /// <param name="command">The command instance to be executed.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>A <see cref="ValueTask{TResponse}"/> representing the asynchronous command result.</returns>
        ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default);


        ValueTask<TResponse> Run<TRequest, TResponse>(TRequest request, CancellationToken ct = default);

    }
}
