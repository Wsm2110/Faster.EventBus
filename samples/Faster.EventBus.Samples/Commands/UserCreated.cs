using Faster.EventBus.Contracts;

namespace Faster.EventBus.Samples.Commands
{
    /// <summary>
    /// Event that is raised after a user was created.
    /// </summary>
    public sealed record UserCreated(string Name) : IEvent;
}
