using Faster.EventBus.Contracts;
using Faster.EventBus.Core;
using Faster.EventBus.Samples.Commands;

namespace Faster.EventBus.Samples.Handlers
{
    internal class SendLogHandler : ICommandHandler<SendLog, Result>
    {
        public ValueTask<Result> Handle(SendLog cmd, CancellationToken ct)
        {
            // Very small "business logic" example.
            if (string.IsNullOrWhiteSpace(cmd.log))
            {
                return new ValueTask<Result>(Result.Failure("log: something went wrong"));
            }

            Console.WriteLine($"log: '{cmd.log}'...");
            return new ValueTask<Result>(Result.Success());
        }
    }
}