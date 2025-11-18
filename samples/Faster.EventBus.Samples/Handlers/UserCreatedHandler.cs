using Faster.EventBus.Contracts;
using Faster.EventBus.Samples.Commands;

namespace Faster.EventBus.Samples.Handlers
{
    /// <summary>
    /// Event handler for UserCreated that just writes to the console.
    /// </summary>
    public sealed class UserCreatedHandler : IEventHandler<UserCreated>
    {
        public ValueTask Handle(UserCreated evt, CancellationToken ct)
        {
            Console.WriteLine($"[UserCreatedHandler] User created event received for '{evt.Name}'.");
            return ValueTask.CompletedTask;
        }
    }
}
