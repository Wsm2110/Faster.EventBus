using Faster.EventBus.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

/// <summary>
/// High-performance dispatcher for executing use cases (CQRS command/query handlers)
/// using expression-tree compiled delegates to avoid reflection and virtual dispatch
/// overhead in the execution path.
/// </summary>
/// <remarks>
/// All expensive reflection, generic resolution, and expression compilation happens
/// once per (TRequest, TResponse) pair and is cached in <see cref="_invokerCache"/>.
/// After that, the hot path contains only:
/// - requestType lookup
/// - ConcurrentDictionary lookup
/// - direct delegate invocation
///
/// No allocations are performed in the publish path.
/// </remarks>
public sealed class UseCaseDispatcher(IServiceProvider provider) : IUseCaseDispatcher
{
    /// <summary>
    /// Cache of compiled invocation delegates keyed by concrete (TRequest, TResponse) type pair.
    /// Value is boxed <see cref="Func{IServiceProvider, IUseCase, CancellationToken, ValueTask}"/>.
    /// </summary>
    private static readonly ConcurrentDictionary<(Type Request, Type Response), object> _invokerCache = new();

    /// <summary>
    /// Executes the use case handler for the given request, resolving the concrete handler
    /// from DI and invoking its Handle method via a cached compiled delegate.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type produced by the use case handler.</typeparam>
    /// <param name="request">The request object executing the use case.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A <see cref="ValueTask{T}"/> representing the asynchronous execution result.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> Execute<TResponse>(
        IUseCase<TResponse> request,
        CancellationToken ct = default)
    {
        // Retrieve runtime request type. (This handles polymorphic request subclasses.)
        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        // Cache key for (TRequest, TResponse)
        var key = (Request: requestType, Response: responseType);

        // Retrieve or build the strongly-typed delegate for executing:
        // (provider, request, ct) => handler.Handle((TRequest)request, ct)
        var invoker = (Func<IServiceProvider, IUseCase<TResponse>, CancellationToken, ValueTask<TResponse>>)
            _invokerCache.GetOrAdd(
                key,
                static k => CompileInvoker<TResponse>(k.Request)
            );

        return invoker(provider, request, ct);
    }

    /// <summary>
    /// Creates a strongly typed compiled delegate for invoking:
    /// <code>
    /// handler = provider.GetRequiredService&lt;IUseCaseHandler&lt;TRequest,TResponse&gt;&gt;();
    /// return handler.Handle((TRequest)request, ct);
    /// </code>
    /// This delegate eliminates reflection, boxing, and virtual dispatch overhead
    /// during use case execution.
    /// </summary>
    /// <typeparam name="TResponse">Response type returned from the request handler.</typeparam>
    /// <param name="requestType">Concrete runtime type of the request.</param>
    /// <returns>A compiled delegate for invoking the handler.</returns>
    private static Func<IServiceProvider, IUseCase<TResponse>, CancellationToken, ValueTask<TResponse>>
        CompileInvoker<TResponse>(Type requestType)
        where TResponse : notnull
    {
        // Parameters for the lambda:
        // (IServiceProvider provider, IUseCase<TResponse> request, CancellationToken ct)
        var providerParam = Expression.Parameter(typeof(IServiceProvider), "provider");
        var requestParam = Expression.Parameter(typeof(IUseCase<TResponse>), "request");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        // IUseCaseHandler<TRequest, TResponse>
        var handlerInterfaceType = typeof(IUseCaseHandler<,>).MakeGenericType(requestType, typeof(TResponse));

        // Build provider.GetRequiredService<IUseCaseHandler<TRequest,TResponse>>()
        var getRequiredServiceMethod = typeof(ServiceProviderServiceExtensions)
            .GetMethods()
            .Single(m =>
                m.Name == nameof(ServiceProviderServiceExtensions.GetRequiredService) &&
                m.IsGenericMethodDefinition &&
                m.GetParameters().Length == 1);

        var getRequiredServiceClosed = getRequiredServiceMethod.MakeGenericMethod(handlerInterfaceType);

        // Expression: provider.GetRequiredService<IUseCaseHandler<TRequest,TResponse>>()
        var resolveCall = Expression.Call(getRequiredServiceClosed, providerParam);

        // Locate the correct Handle() method:
        var handleMethod = handlerInterfaceType.GetMethod(nameof(IUseCaseHandler<IUseCase<TResponse>, TResponse>.Handle))
                           ?? handlerInterfaceType.GetMethod("Handle")!;

        // Cast request to TRequest
        var castRequest = Expression.Convert(requestParam, requestType);

        // Expression: handler.Handle((TRequest)request, ct)
        var handleCall = Expression.Call(resolveCall, handleMethod, castRequest, ctParam);

        // Compile lambda:
        // (provider, request, ct) => handler.Handle((TRequest)request, ct)
        var lambda = Expression.Lambda<Func<IServiceProvider, IUseCase<TResponse>, CancellationToken, ValueTask<TResponse>>>(
            handleCall,
            providerParam,
            requestParam,
            ctParam
        );

        return lambda.Compile();
    }
}
