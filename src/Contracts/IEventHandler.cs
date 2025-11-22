using System;
using System.Collections.Generic;
using System.Text;

namespace Faster.EventBus.Contracts;

/// <summary>
/// Handles an event. There can be many handlers for one event type.
/// </summary>
/// <typeparam name="TEvent">Event type.</typeparam>
public interface IEventHandler<TEvent>
    where TEvent : IEvent
{
    /// <summary>
    /// Handles the given event.
    /// </summary>
    ValueTask Handle(TEvent evt, CancellationToken ct);
}
