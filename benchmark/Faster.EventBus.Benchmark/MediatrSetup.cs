using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;

public sealed class TestMediatrCommand : IRequest<string>
{
    public int Id { get; }
    public TestMediatrCommand(int id) => Id = id;
}

public sealed class TestMediatrHandler :
    IRequestHandler<TestMediatrCommand, string>
{
    public Task<string> Handle(TestMediatrCommand request, CancellationToken cancellationToken)
        => Task.FromResult($"User-{request.Id}");
}

public sealed class MedBehavior<TRequest, TResponse> :
    IPipelineBehavior<TRequest, TResponse>
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
        => next();
}

public static class MediatRSetup
{
    public static IMediator Build()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(TestMediatrCommand).Assembly));

        services.AddTransient<IRequestHandler<TestMediatrCommand, string>, TestMediatrHandler>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MedBehavior<,>));

        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }
}
