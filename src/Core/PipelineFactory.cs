using System.Reflection;
using Faster.EventBus.Contracts;

/// <summary>
/// Builds the command execution pipeline for a given command type.
/// The pipeline consists of:
///   - the final command handler
///   - zero or more pipeline behaviors (middleware)
/// It returns a compiled delegate that can be executed very fast at runtime.
/// </summary>
internal static class PipelineFactory
{
    /// <summary>
    /// Builds a delegate for the given <paramref name="commandType"/> and <paramref name="responseType"/>.
    /// This method is called at startup (or when registering handlers), NOT on each Send.
    /// It uses reflection once to create a strongly-typed generic version of the builder.
    /// </summary>
    /// <param name="commandType">Concrete command type, e.g. GetUserNameCommand.</param>
    /// <param name="handler">The resolved handler instance for this command.</param>
    /// <param name="behaviors">An array of pipeline behaviors (can be empty).</param>
    /// <param name="responseType">The response type returned by the command handler.</param>
    /// <returns>
    /// A delegate with the shape:
    /// <c>Func&lt;ICommand&lt;TResponse&gt;, CancellationToken, ValueTask&lt;TResponse&gt;&gt;</c>
    /// stored as a <see cref="Delegate"/> for caching.
    /// </returns>
    public static Delegate Build(
        Type commandType,
        object handler,
        object[] behaviors,
        Type responseType)
    {
        // We call the generic version:     
        var method = typeof(PipelineFactory)
            .GetMethod(nameof(BuildGeneric), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(commandType, responseType);

        return (Delegate)method.Invoke(null, new object[] { handler, behaviors })!;
    }

    /// <summary>
    /// Builds a strongly-typed pipeline delegate for a specific command and response.
    /// This returns a delegate with the input type <see cref="ICommand{TResponse}"/> so we
    /// can safely call it from EventDispatcher.Send(ICommand&lt;TResponse&gt;).
    /// </summary>
    /// <typeparam name="TCommand">The concrete command type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="handlerObj">The ICommandHandler&lt;TCommand,TResponse&gt; instance (boxed as object).</param>
    /// <param name="behaviors">Array of ICommandPipelineBehavior&lt;TCommand,TResponse&gt; (boxed).</param>
    /// <returns>
    /// A delegate: <c>Func&lt;ICommand&lt;TResponse&gt;, CancellationToken, ValueTask&lt;TResponse&gt;&gt;</c>.
    /// </returns>
    private static Func<ICommand<TResponse>, CancellationToken, ValueTask<TResponse>> BuildGeneric<TCommand, TResponse>(
        object handlerObj,
        object[] behaviors)
        where TCommand : ICommand<TResponse>
    {
        // Cast handler to the correct interface type once
        var handler = (ICommandHandler<TCommand, TResponse>)handlerObj;

        // Base "core" delegate just calls the handler directly:      
        Func<TCommand, CancellationToken, ValueTask<TResponse>> core = handler.Handle;

        // If we have behaviors, we wrap them around the core like middleware.
        if (behaviors is { Length: > 0 })
        {
            // Cast behaviors to the correct generic type once.
            var typedBehaviors = new ICommandPipelineBehavior<TCommand, TResponse>[behaviors.Length];
            for (int i = 0; i < behaviors.Length; i++)
            {
                typedBehaviors[i] = (ICommandPipelineBehavior<TCommand, TResponse>)behaviors[i];
            }

            // Wrap behaviors from last to first, so the first in the list is the outermost.
            for (int i = typedBehaviors.Length - 1; i >= 0; i--)
            {
                var behavior = typedBehaviors[i];
                var next = core; // capture the current "next" delegate

                core = (cmd, ct) =>
                {
                    // This delegate will be executed inside the behavior.
                    CommandHandlerDelegate<TResponse> nextDelegate = () => next(cmd, ct);
                    return behavior.Handle(cmd, ct, nextDelegate);
                };
            }
        }

        // ADAPTER:
        // We want the final delegate to accept ICommand<TResponse>, not TCommand,
        // so EventDispatcher.Send(ICommand<TResponse>) can call it safely.
        return (ICommand<TResponse> command, CancellationToken ct) =>
        {
            // We know command is actually TCommand here, so we cast once and call the core pipeline.
            return core((TCommand)command, ct);
        };
    }
}