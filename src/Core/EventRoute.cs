using Faster.EventBus.Contracts;
using System.Collections.Immutable;

namespace Faster.EventBus.Core
{
    /// <summary>
    /// Manages subscriptions and publishing for a specific event type.
    /// Uses ImmutableArray so that publishing is lock-free.
    /// </summary>
    /// <typeparam name="TEvent">Event type.</typeparam>
    internal static class EventRoute<TEvent> where TEvent : IEvent
    {
        private static ImmutableArray<Func<TEvent, CancellationToken, ValueTask>> _subscribers = ImmutableArray<Func<TEvent, CancellationToken, ValueTask>>.Empty;

        /// <summary>
        /// Subscribes a handler function to this event type.
        /// </summary>
        public static void Subscribe(Func<TEvent, CancellationToken, ValueTask> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            ImmutableInterlocked.Update(ref _subscribers,
                static (current, h) => current.Add(h),
                handler);
        }

        /// <summary>
        /// Publishes the event to all subscribers.
        /// </summary>
        public async static ValueTask Publish(TEvent evt, CancellationToken ct)
        {
            var snapshot = _subscribers;
            if (snapshot.IsDefaultOrEmpty)
            {
                return;
            }

            var tasks = snapshot.Select(sub => sub(evt, ct).AsTask()).ToArray();

            // Do NOT await them. This initiates the work and returns immediately.
            // Use Task.Run and ConfigureAwait(false) to offload the continuation 
            // and prevent unobserved exceptions from crashing the process (in older .NET versions).
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // IMPORTANT: Log all handler exceptions here!
                    // Without this, exceptions are unobserved.
                    Console.WriteLine($"Event handler failed: {ex.Message}");
                }
            });          
        }
    }
}