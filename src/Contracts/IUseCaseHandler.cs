using System;
using System.Threading;
using System.Threading.Tasks;

namespace Faster.EventBus.Contracts
{
    // ============================================================================
    // USE CASES, COMMANDS & HANDLERS - DO & DON'T GUIDELINES
    // ============================================================================
    //
    // DO:
    // - Use a Command + Handler for a single atomic operation.
    // - Use a UseCase to orchestrate multiple steps.
    // - Use a UseCase when calling functionality across modules.
    // - Call Commands only from inside UseCases.
    // - Expose only UseCases as public module entry points.
    // - Keep CommandHandlers small and focused.
    // - Return early inside a UseCase when a step fails.
    // - Treat Commands and Handlers as internal implementation details.
    // - Use the dispatcher inside UseCases (not inside handlers).
    //
    // DON'T:
    // - Do NOT call a handler from another handler.
    // - Do NOT call commands inside handlers using EventBus/Dispatcher.
    // - Do NOT call commands across modules.
    // - Do NOT put workflow/orchestration logic inside handlers.
    // - Do NOT create a UseCase if it does only one internal step.
    // - Do NOT expose command handlers across module boundaries.
    // - Do NOT let command handlers become mini-usecases.
    //
    // Golden rules:
    // - Handlers execute one atomic business operation.
    // - UseCases orchestrate workflow and call commands.
    // - Modules communicate only through UseCases.
    // - No handler -> handler (direct or indirect via dispatcher).
    //
    // ============================================================================

    /// <summary>
    /// Defines a handler responsible for processing a request of type <typeparamref name="TRequest"/>
    /// and producing a response of type <typeparamref name="TResponse"/>. 
    /// This represents the core execution logic of a use case in the application.
    /// </summary>
    /// <typeparam name="TRequest">The type of request data to be processed.</typeparam>
    /// <typeparam name="TResponse">The type of response returned after processing the request.</typeparam>
    public interface IUseCaseHandler<TRequest, TResponse>
    {
        /// <summary>
        /// Handles the given request and returns a response asynchronously.
        /// </summary>
        /// <param name="request">The incoming request to process.</param>
        /// <param name="ct">Optional cancellation token for cooperative cancellation.</param>
        /// <returns>
        /// A <see cref="ValueTask{TResponse}"/> containing the result of the operation.
        /// </returns>
        ValueTask<TResponse> Handle(TRequest request, CancellationToken ct = default);
    }
}
