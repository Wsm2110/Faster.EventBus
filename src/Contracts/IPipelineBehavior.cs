using System;
using System.Threading;

namespace Faster.EventBus.Contracts
{
    /// <summary>
    /// Represents a pipeline behavior (middleware) that wraps around
    /// command execution. Behaviors allow additional logic to run before
    /// or after the command handler executes.
    ///
    /// This is similar to ASP.NET middleware or decorators.
    /// Examples of behaviors include:
    ///   - Validation
    ///   - Logging
    ///   - Metrics / tracing
    ///   - Authorization
    ///   - Retry / fallback logic
    ///
    /// Behaviors form a chain where each behavior chooses whether and when
    /// to call the next behavior in the pipeline using the <paramref name="next"/> delegate.
    /// The last behavior ultimately calls the command handler.
    ///
    /// Execution flow:
    ///   Behavior1 -> Behavior2 -> Behavior3 -> Handler
    ///
    /// Order matters: the first behavior wraps everything inside.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command being processed.</typeparam>
    /// <typeparam name="TResponse">The type returned by executing the command.</typeparam>
    public interface IPipelineBehavior<in TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        /// <summary>
        /// Handles the command or delegates execution to the next step in the pipeline.
        /// A behavior can inspect or modify the command, perform logic before or after
        /// the handler runs, or even prevent execution entirely.
        /// </summary>
        /// <param name="command">The command being processed.</param>
        /// <param name="next">
        /// Delegate representing the next step in the pipeline.
        /// Calling <c>next()</c> continues pipeline execution,
        /// and if this is the last behavior it will invoke the actual command handler.
        /// </param>
        /// <param name="ct">Cancellation token used to cancel processing.</param>
        /// <returns>
        /// A <see cref="ValueTask{TResponse}"/> representing asynchronous processing.
        /// Contains the result from the handler or modified result from the behavior.
        /// </returns>
        ValueTask<TResponse> Handle(
            TCommand command,
            CommandBehaviorDelegate<TResponse> next,
            CancellationToken ct
        );
    }
}
