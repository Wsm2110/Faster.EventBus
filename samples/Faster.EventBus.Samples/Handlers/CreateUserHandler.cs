using Faster.EventBus.Contracts;
using Faster.EventBus.Core;
using Faster.EventBus.Samples.Commands;
using Faster.EventBus.Shared;

namespace Faster.EventBus.Samples.Handlers
{
    /// <summary>
    /// Handles the CreateUser command.
    /// </summary>
    public sealed class CreateUserHandler : ICommandHandler<CreateUser, Result>
    {
        public ValueTask<Result> Handle(CreateUser command, CancellationToken ct)
        {
            // Very small "business logic" example.
            if (string.IsNullOrWhiteSpace(command.Name))
            {
                return new ValueTask<Result>(Result.Failure("Name is required."));
            }

            Console.WriteLine($"[CreateUserHandler] Creating user '{command.Name}'...");
            return new ValueTask<Result>(Result.Success());
        }
    }
}
