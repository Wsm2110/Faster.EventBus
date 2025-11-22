using Faster.EventBus.Contracts;
using Faster.EventBus.Shared;
using System.Threading;
using System.Threading.Tasks;

public record TestResultCommand(int Id) : ICommand<Result<string>>;

public sealed class TestResultHandler :
    ICommandHandler<TestResultCommand, Result<string>>
{
    public ValueTask<Result<string>> Handle(TestResultCommand cmd, CancellationToken ct)
        => ValueTask.FromResult(Result<string>.Success($"User-{cmd.Id}"));
}

