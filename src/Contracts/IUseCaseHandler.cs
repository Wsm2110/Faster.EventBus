using System;
using System.Threading;
using System.Threading.Tasks;

namespace Faster.EventBus.Contracts
{
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
