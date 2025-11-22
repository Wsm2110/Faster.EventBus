using Faster.EventBus.Contracts;
using Faster.EventBus.Core;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

/// <summary>
/// High-performance command sender that finds and executes the correct
/// command handler pipeline for a command. Pipelines are created lazily
/// and then cached, so after the first run there is zero allocation and
/// no reflection in the hot path.
/// </summary>
public sealed class CommandDispatcher(IServiceProvider Provider, IPipelineFactory PipelineFactory) : ICommandDispatcher
{
    /// <summary>
    /// Cache that stores the compiled pipeline delegate for each command type.
    /// Key: CommandType → delegate that executes the handler sequence
    /// </summary>
    private static readonly ConcurrentDictionary<Type, object> _pipelineCache = new();

    /// <summary>
    /// Sends a command and returns the response. The correct pipeline is chosen
    /// based on the runtime type of the command.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
    {
        // Determine actual runtime type (important for polymorphism)
        var type = command.GetType();
        var pipeline = _pipelineCache.GetOrAdd(type, t =>
        {
            return PipelineFactory.CreateDelegate<TResponse>(t, Provider);
        });

        // The rest of the execution logic remains the same (and fast)
        var handler = (CommandPipeline<TResponse>)pipeline;
        return handler(Provider, command, ct);
    }
}
