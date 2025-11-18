using System;
using System.Threading;
using System.Threading.Tasks;

namespace Faster.EventBus.Contracts
{
    /// <summary>
    /// Represents a command that expects a response of type <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response, usually Result.</typeparam>
    public interface ICommand<TResponse> { }

    /// <summary>
    /// Marker interface for an event (publish/subscribe).
    /// </summary>
    public interface IEvent { }

    /// <summary>
    /// Handles a command and returns a ValueTask of <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command.</typeparam>
    /// <typeparam name="TResponse">Type of the response, usually Result.</typeparam>
    public interface ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        /// <summary>
        /// Handles the given command.
        /// </summary>
        ValueTask<TResponse> Handle(TCommand command, CancellationToken ct);
    }

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

    /// <summary>
    /// Delegate used by pipeline behaviors to call the next element in the chain.
    /// </summary>
    /// <typeparam name="TResponse">Type of response, usually Result.</typeparam>
    public delegate ValueTask<TResponse> CommandHandlerDelegate<TResponse>();

    /// <summary>
    /// Represents a pipeline behavior (middleware) that wraps command handling,
    /// for example for logging, validation, timing, retry, etc.
    /// </summary>
    /// <typeparam name="TCommand">Command type.</typeparam>
    /// <typeparam name="TResponse">Response type, usually Result.</typeparam>
    public interface ICommandPipelineBehavior<TCommand, TResponse> where TCommand : ICommand<TResponse>
    {
        /// <summary>
        /// Handles the command and either processes it or calls the next delegate.
        /// </summary>
        ValueTask<TResponse> Handle(
            TCommand command,
            CancellationToken ct,
            CommandHandlerDelegate<TResponse> next);
    }
}
