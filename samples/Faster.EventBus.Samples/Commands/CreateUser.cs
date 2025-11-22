using Faster.EventBus.Contracts;
using Faster.EventBus.Core;
using Faster.EventBus.Shared;

namespace Faster.EventBus.Samples.Commands
{
    /// <summary>
    /// Command asking the system to create a new user.
    /// </summary>
    public sealed record CreateUser(string Name) : ICommand<Result>;

    public sealed record SendLog(string log) : ICommand<Result>;
}
