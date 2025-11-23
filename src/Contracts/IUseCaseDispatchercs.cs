using System;
using System.Threading;
using System.Threading.Tasks;

namespace Faster.EventBus.Contracts
{
    /// <summary>
    /// Defines a dispatcher capable of executing use cases that follow a request/response pattern.
    /// A use case is represented by an object implementing <see cref="IUseCase{TResponse}"/>,
    /// and the dispatcher resolves and invokes the appropriate <c>IUseCaseHandler&lt;TRequest,TResponse&gt;</c>
    /// based on the runtime type of the request.
    /// </summary>
    public interface IUseCaseDispatcher
    {
        /// <summary>
        /// Executes a use case request by resolving its corresponding handler from the dependency injection container
        /// and invoking its <c>Handle</c> method. Produces a response of type <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TResponse">The type of response returned by the use case.</typeparam>
        /// <param name="request">The request instance representing the use case to be executed.</param>
        /// <param name="ct">Optional cancellation token used for cooperative cancellation.</param>
        /// <returns>A <see cref="ValueTask{TResponse}"/> representing the asynchronous operation result.</returns>
        ValueTask<TResponse> Execute<TResponse>(
            IUseCase<TResponse> request,
            CancellationToken ct = default);
    }
}
