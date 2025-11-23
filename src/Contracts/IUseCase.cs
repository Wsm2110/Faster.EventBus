using System;

/// <summary>
/// Marker interface representing a use case that produces a response of type <typeparamref name="TResponse"/>.
/// This is the base abstraction for request/response operations in the application.
/// </summary>
/// <typeparam name="TResponse">
/// The response type returned after the use case completes execution.
/// </typeparam>
public interface IUseCase<TResponse>
{
}

/// <summary>
/// Represents a strongly typed use case that includes a concrete request payload of type
/// <typeparamref name="TRequest"/> and produces a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TRequest">The request payload type consumed by the use case.</typeparam>
/// <typeparam name="TResponse">The response type produced by the execution of the use case.</typeparam>
/// <remarks>
/// This interface enables:
/// <list type="bullet">
/// <item><description>Strong typing for request data</description></item>
/// <item><description>Clear separation of request and response models</description></item>
/// <item><description>Support for dispatching logic to generically resolve handlers based on request type</description></item>
/// </list>
/// </remarks>
public interface IUseCase<TRequest, TResponse> : IUseCase<TResponse>
{
    /// <summary>
    /// Gets the request payload associated with the current use case.
    /// This value is typically populated when sending the use case through the dispatcher.
    /// </summary>
    TRequest Request { get; }
}
