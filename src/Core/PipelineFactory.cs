using Faster.EventBus.Contracts;
using Faster.EventBus.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// The PipelineFactory is responsible for building and caching delegates
/// that execute command pipelines (CommandHandlers + Pipeline Behaviors)
/// for the Faster.EventBus system.
///
/// This version is optimized for extreme performance:
/// - No reflection in the hot path
/// - No dictionary lookups for handler invokers
/// - Very low allocations during pipeline execution
/// - Uses FastExpressionCompiler for fast startup compilation
/// </summary>
public sealed class PipelineFactory : IPipelineFactory
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Cache that stores compiled pipeline delegates for (CommandType, ResponseType) pairs.
    /// This avoids rebuilding the pipeline multiple times.
    /// </summary>
    private static readonly ConcurrentDictionary<(Type, Type), object> _pipelineCache = new();

    // NOTE: We no longer need a handler dictionary cache.
    // Handler invokers are cached per TCommand/TResponse using a static generic type.

    public PipelineFactory(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Creates a delegate that will execute a pipeline for a specific command type.
    /// The pipeline may contain zero or more middleware behaviors and one handler.
    /// </summary>
    public CommandPipeline<TResponse> CreateDelegate<TResponse>(Type commandType, IServiceProvider rootProvider)
    {
        // Pipeline delegate is cached per (commandType, responseType)
        return (CommandPipeline<TResponse>)_pipelineCache.GetOrAdd(
            (commandType, typeof(TResponse)),
            key => CompilePipeline<TResponse>(key.Item1, key.Item2)
        );
    }

    /// <summary>
    /// Builds the pipeline execution delegate for a command/response pair
    /// and binds it to this PipelineFactory instance.
    /// Called only once per command type.
    /// </summary>
    private CommandPipeline<TResponse> CompilePipeline<TResponse>(Type commandType, Type responseType)
    {
        var method = typeof(PipelineFactory)
            .GetMethod(nameof(ExecutePipeline), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(commandType, responseType);

        // Convert the method into a strongly typed delegate and return it.
        return (CommandPipeline<TResponse>)method.CreateDelegate(typeof(CommandPipeline<TResponse>), this);
    }

    /// <summary>
    /// Runs the pipeline chain for a command.
    /// Resolves behaviors and calls them one by one until it reaches the handler.
    /// </summary>
    private ValueTask<TResponse> ExecutePipeline<TCommand, TResponse>(
        IServiceProvider provider,
        ICommand<TResponse> command,
        CancellationToken ct)
        where TCommand : ICommand<TResponse>
    {
        // Resolve all behaviors for this command type
        var behaviors = (IPipelineBehavior<TCommand, TResponse>[])
            provider.GetServices<IPipelineBehavior<TCommand, TResponse>>();

        var handlerInvoker = HandlerInvokerCache<TCommand, TResponse>.Invoker;

        // Fast path: no behaviors, go straight to handler
        if (behaviors.Length == 0)
        {
            return handlerInvoker(provider, (TCommand)command, ct);
        }

        // Use a small state object + a single next delegate to avoid
        // allocating a new lambda for each behavior.
        var state = new PipelineState<TCommand, TResponse>(
            provider,
            (TCommand)command,
            ct,
            behaviors,
            handlerInvoker);

        return state.Run();
    }

    /// <summary>
    /// Holds per-invocation pipeline state, so we only allocate:
    /// - one small object (this)
    /// - one CommandBehaviorDelegate&lt;TResponse&gt; for "next"
    /// instead of one lambda allocation per behavior.
    /// </summary>
    private sealed class PipelineState<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        private readonly IServiceProvider _provider;
        private readonly TCommand _command;
        private readonly CancellationToken _ct;
        private readonly IPipelineBehavior<TCommand, TResponse>[] _behaviors;
        private readonly Func<IServiceProvider, TCommand, CancellationToken, ValueTask<TResponse>> _handler;
        private int _index;

        private readonly CommandBehaviorDelegate<TResponse> _nextDelegate;

        public PipelineState(
            IServiceProvider provider,
            TCommand command,
            CancellationToken ct,
            IPipelineBehavior<TCommand, TResponse>[] behaviors,
            Func<IServiceProvider, TCommand, CancellationToken, ValueTask<TResponse>> handler)
        {
            _provider = provider;
            _command = command;
            _ct = ct;
            _behaviors = behaviors;
            _handler = handler;

            // Create a single delegate that always calls NextCore()
            _nextDelegate = NextCore;
        }

        public ValueTask<TResponse> Run() => NextCore();

        private ValueTask<TResponse> NextCore()
        {
            // If we've exhausted all behaviors, call the handler
            if (_index == _behaviors.Length)
            {
                return _handler(_provider, _command, _ct);
            }

            // Otherwise execute the current behavior and delegate to the same next delegate
            var behavior = _behaviors[_index++];
            return behavior.Handle(_command, _nextDelegate, _ct);
        }
    }

    /// <summary>
    /// Static generic cache for the handler invoker delegate:
    ///   (provider, command, ct) => handler.Handle(command, ct)
    ///
    /// Cached per TCommand/TResponse, so there is:
    ///   - no dictionary in the hot path
    ///   - no reflection after first build
    /// </summary>
    private static class HandlerInvokerCache<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public static readonly Func<IServiceProvider, TCommand, CancellationToken, ValueTask<TResponse>> Invoker
            = CompileHandlerDelegate();

        private static Func<IServiceProvider, TCommand, CancellationToken, ValueTask<TResponse>> CompileHandlerDelegate()
        {
            // (IServiceProvider provider, TCommand command, CancellationToken ct) => 
            //      provider.GetRequiredService<ICommandHandler<TCommand,TResponse>>().Handle(command, ct);

            var providerParam = Expression.Parameter(typeof(IServiceProvider), "provider");
            var commandParam = Expression.Parameter(typeof(TCommand), "command");
            var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

            var handlerType = typeof(ICommandHandler<TCommand, TResponse>);

            // Find generic GetRequiredService<T>() extension method
            MethodInfo? getReqGeneric = null;
            var methods = typeof(ServiceProviderServiceExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static);

            for (int i = 0; i < methods.Length; i++)
            {
                var m = methods[i];
                if (m.Name == nameof(ServiceProviderServiceExtensions.GetRequiredService)
                    && m.IsGenericMethodDefinition
                    && m.GetParameters().Length == 1)
                {
                    getReqGeneric = m;
                    break;
                }
            }

            if (getReqGeneric == null)
            {
                throw new InvalidOperationException("Could not find GetRequiredService<T>(IServiceProvider) method.");
            }

            var getReqMethod = getReqGeneric.MakeGenericMethod(handlerType);

            // provider.GetRequiredService<ICommandHandler<TCommand,TResponse>>()
            var handlerCall = Expression.Call(getReqMethod, providerParam);

            // handler.Handle(command, ct)
            var handleMethod = handlerType.GetMethod("Handle")!;
            var handleCall = Expression.Call(handlerCall, handleMethod, commandParam, ctParam);

            var lambda = Expression.Lambda<
                Func<IServiceProvider, TCommand, CancellationToken, ValueTask<TResponse>>
            >(handleCall, providerParam, commandParam, ctParam);

            // FastExpressionCompiler: much faster cold compile than Expression.Compile()
            return lambda.Compile()!;
        }
    }
}
