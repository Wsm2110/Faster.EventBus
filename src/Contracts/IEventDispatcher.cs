using System;
using System.Threading;

namespace Faster.EventBus.Contracts
{
    /// <summary>
    /// Represents a simplified abstraction for publishing events in the system.
    ///
    /// An event describes something that has already happened
    /// (for example: <c>UserRegisteredEvent</c>, <c>OrderShippedEvent</c>).
    ///
    /// Unlike commands, which must have exactly one handler and produce a result,
    /// events may:
    ///  - Have zero handlers (no subscribers)
    ///  - Have one handler
    ///  - Have many handlers that respond independently
    ///
    /// Implementations of this interface are responsible for locating all
    /// matching <see cref="IEventHandler{TEvent}"/> instances and invoking them.
    /// </summary>
    public interface IEventDispatcher
    {
        /// <summary>
        /// Publishes an event so all registered handlers can process it.
        /// Execution order is not guaranteed, and each handler is independent.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to publish.</typeparam>
        /// <param name="event">The event instance that contains event data.</param>
        /// <param name="ct">Optional cancellation token used to cancel event handling.</param>
        /// <returns>A <see cref="Task"/> representing asynchronous completion.</returns>
        Task Publish<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IEvent;
    }
}
