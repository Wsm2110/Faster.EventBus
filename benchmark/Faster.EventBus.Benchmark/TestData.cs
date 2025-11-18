using Faster.EventBus.Contracts;
using Faster.EventBus.Core;
using System.Threading;
using System.Threading.Tasks;

public record TestResultCommand(int Id) : ICommand<Result<string>>;

public sealed class TestResultHandler :
    ICommandHandler<TestResultCommand, Result<string>>
{
    public ValueTask<Result<string>> Handle(TestResultCommand cmd, CancellationToken ct)
        => ValueTask.FromResult(Result<string>.Success($"User-{cmd.Id}"));
}

public sealed class ResultBehavior :
    ICommandPipelineBehavior<TestResultCommand, Result<string>>
{
    public async ValueTask<Result<string>> Handle(
        TestResultCommand cmd,
        CancellationToken ct,
        CommandHandlerDelegate<Result<string>> next)
    {
        return await next();
    }
}
