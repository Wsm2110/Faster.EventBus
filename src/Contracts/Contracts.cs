namespace Faster.EventBus.Contracts;

///
/// <summary>
/// Represents the delegate used by the core command handler.
/// This is the final step in the pipeline: it executes the real command handler.
/// 
/// When the pipeline reaches the end (no more behaviors to process),
/// this delegate is invoked to run:
///     handler.Handle(command, ct)
///
/// It returns a <see cref="ValueTask{TResponse}"/> because command handlers
/// may run asynchronously and we want minimal allocation overhead.
/// </summary>
/// <typeparam name="TResponse">The type returned by the command handler.</typeparam>
public delegate ValueTask<TResponse> CommandHandlerDelegate<TResponse>();


/// <summary>
/// Represents the delegate passed to each pipeline behavior so that
/// the behavior can invoke the next step in the pipeline.
///
/// Each behavior receives a function called <c>next</c>, which either:
///   - Calls the next behavior in the chain, or
///   - Calls the core command handler if this is the last behavior.
///
/// This enables middleware patterns such as logging, validation, metrics,
/// retries, authorization, etc.
/// </summary>
/// <typeparam name="TResponse">The response type returned by the overall pipeline.</typeparam>
public delegate ValueTask<TResponse> CommandBehaviorDelegate<TResponse>();


/// <summary>
/// Represents the compiled pipeline delegate that executes the full pipeline,
/// constructed by the <see cref="IPipelineFactory"/> and stored in cache.
///
/// This delegate is invoked by the command sender and performs the full sequence:
///   1. Run all pipeline behaviors in order (if any)
///   2. Finally execute the command handler
///
/// All required dependencies are passed in because this delegate may be reused
/// many times without needing to re-resolve dependencies.
///
/// <example>
/// Example internal pipeline execution chain:
///     LoggingBehavior → ValidationBehavior → Handler
/// </example>
/// </summary>
/// <typeparam name="TResponse">The type returned from the command handler.</typeparam>
/// <param name="provider">The scoped DI provider used to resolve services.</param>
/// <param name="command">The command instance being executed.</param>
/// <param name="ct">Optional cancellation token.</param>
/// <returns>A <see cref="ValueTask{TResponse}"/> representing the result.</returns>
public delegate ValueTask<TResponse> CommandPipeline<TResponse>(
    IServiceProvider provider,
    ICommand<TResponse> command,
    CancellationToken ct
);

public delegate ValueTask<TResponse> UseCasePipeline<TResponse>(
    IServiceProvider provider,
    object request,
    CancellationToken ct);