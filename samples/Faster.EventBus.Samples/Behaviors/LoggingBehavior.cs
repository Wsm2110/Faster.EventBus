using Faster.EventBus.Contracts;
using Faster.EventBus.Core;
using Faster.EventBus.Samples.Commands;

namespace Faster.EventBus.Samples.Behaviors
{
    /// <summary>
    /// Simple logging behavior that wraps command execution.
    /// </summary>
    /// <typeparam name="TCommand">Command type.</typeparam>
    public sealed class LoggingBehavior : ICommandPipelineBehavior<CreateUser, Result>      
    {  
        public async ValueTask<Result> Handle(
            CreateUser command,
            CancellationToken ct,
            CommandHandlerDelegate<Result> next)
        {
            Console.WriteLine($"[LoggingBehavior] Handling {typeof(CreateUser).Name}...");
            var result = await next().ConfigureAwait(false);
            Console.WriteLine($"[LoggingBehavior] Done {typeof(CreateUser).Name}: {result}");
            return result;
        }
    }
}
