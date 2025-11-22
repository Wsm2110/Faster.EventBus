using Faster.EventBus.Contracts;
using Faster.EventBus.Core;
using Faster.EventBus.Samples.Commands;
using Faster.EventBus.Shared;

namespace Faster.EventBus.Samples.Behaviors
{
    /// <summary>
    /// Simple logging behavior that wraps command execution.
    /// </summary>
    /// <typeparam name="TCommand">Command type.</typeparam>
    public sealed class LoggingBehavior : IPipelineBehavior<CreateUser, Result>
    {
        public async ValueTask<Result> Handle(CreateUser command, CommandBehaviorDelegate<Result> next, CancellationToken ct)
        {
            Console.WriteLine($"[LoggingBehavior] Handling {typeof(CreateUser).Name}...");
            var result = await next();
            Console.WriteLine($"[LoggingBehavior] Done {typeof(CreateUser).Name}: {result}");
            return result;
        }
    }
}
