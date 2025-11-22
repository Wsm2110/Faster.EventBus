using Faster.EventBus.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Faster.EventBus.Core;

public sealed class UseCaseDispatcher(IServiceProvider Provider) : IUseCaseDispatcher
{
    private static readonly ConcurrentDictionary<Type, object> _cache = new();

    public ValueTask<TResponse> Execute<TRequest, TResponse>(
        TRequest request,
        CancellationToken ct = default)
    {
        var requestType = typeof(TRequest);

        // Get cached compiled delegate
        var invoker = (Func<IServiceProvider, TRequest, CancellationToken, ValueTask<TResponse>>)
            _cache.GetOrAdd(requestType, _ => CompileInvoker<TRequest, TResponse>());

        return invoker(Provider, request, ct);
    }

    private static Func<IServiceProvider, TRequest, CancellationToken, ValueTask<TResponse>>
        CompileInvoker<TRequest, TResponse>()
    {
        var providerParam = Expression.Parameter(typeof(IServiceProvider), "provider");
        var requestParam = Expression.Parameter(typeof(TRequest), "request");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        var handlerType = typeof(IUseCaseHandler<TRequest, TResponse>);

        var resolveMethod = typeof(ServiceProviderServiceExtensions)
     .GetMethods(BindingFlags.Public | BindingFlags.Static)
     .Single(m =>
         m.Name == nameof(ServiceProviderServiceExtensions.GetRequiredService) &&
         m.IsGenericMethodDefinition &&
         m.GetParameters().Length == 1)
     .MakeGenericMethod(handlerType);

        var resolveCall = Expression.Call(resolveMethod, providerParam);

        var handleMethod = handlerType.GetMethod(nameof(IUseCaseHandler<TRequest, TResponse>.Handle))!;

        var handleCall = Expression.Call(resolveCall, handleMethod, requestParam, ctParam);

        return Expression
            .Lambda<Func<IServiceProvider, TRequest, CancellationToken, ValueTask<TResponse>>>(
                handleCall, providerParam, requestParam, ctParam)
            .Compile();
    }
}