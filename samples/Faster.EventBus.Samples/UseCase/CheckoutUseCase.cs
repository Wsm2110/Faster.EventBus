using System;
using System.Threading;
using System.Threading.Tasks;
using Faster.EventBus.Contracts;
using Faster.EventBus.Shared;
using Microsoft.Extensions.DependencyInjection;

public sealed record CheckoutRequest(Guid OrderId);

public sealed record PlaceOrderCommand(Guid OrderId) : ICommand<Result>;
public sealed record ChargePaymentCommand(Guid OrderId) : ICommand<Result>;
public sealed record ShipOrderCommand(Guid OrderId) : ICommand<Result>;

public sealed class PlaceOrderHandler :
    ICommandHandler<PlaceOrderCommand, Result>
{
    public ValueTask<Result> Handle(PlaceOrderCommand command, CancellationToken ct)
    {
        Console.WriteLine($"Placing order {command.OrderId}");
        return ValueTask.FromResult(Result.Success());
    }
}

public sealed class ChargePaymentHandler :
    ICommandHandler<ChargePaymentCommand, Result>
{
    public ValueTask<Result> Handle(ChargePaymentCommand command, CancellationToken ct)
    {
        Console.WriteLine($"Charging payment for order {command.OrderId}");
        return ValueTask.FromResult(Result.Success());
    }
}

public sealed class ShipOrderHandler :
    ICommandHandler<ShipOrderCommand, Result>
{
    public ValueTask<Result> Handle(ShipOrderCommand command, CancellationToken ct)
    {
        Console.WriteLine($"Shipping order {command.OrderId}");
        return ValueTask.FromResult(Result.Success());
    }
}

// ======================================================
// Checkout Use Case
// ======================================================

public sealed class CheckoutUseCase :
    IUseCaseHandler<CheckoutRequest, Result>
{
    private readonly ICommandDispatcher _dispatcher;

    public CheckoutUseCase(ICommandDispatcher dispatcher)
        => _dispatcher = dispatcher;

    public async ValueTask<Result> Handle(CheckoutRequest request, CancellationToken ct)
    {
        var placed = await _dispatcher.Send(new PlaceOrderCommand(request.OrderId), ct);
        if (!placed.IsSuccess) return placed;

        var paid = await _dispatcher.Send(new ChargePaymentCommand(request.OrderId), ct);
        if (!paid.IsSuccess) return paid;

        return await _dispatcher.Send(new ShipOrderCommand(request.OrderId), ct);
    }
}


public sealed class FakeCommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _provider;

    public FakeCommandDispatcher(IServiceProvider provider)
        => _provider = provider;

    public async ValueTask<TResponse> Send<TResponse>(
        ICommand<TResponse> command, CancellationToken ct = default)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
        dynamic handler = _provider.GetRequiredService(handlerType);
        return await handler.Handle((dynamic)command, ct);
    }
}

public sealed class FakeUseCaseDispatcher : IUseCaseDispatcher
{
    private readonly IServiceProvider _provider;

    public FakeUseCaseDispatcher(IServiceProvider provider)
        => _provider = provider;

    public async ValueTask<TResponse> Execute<TRequest, TResponse>(TRequest request, CancellationToken ct = default)
    {
        var handler = _provider.GetRequiredService<IUseCaseHandler<TRequest, TResponse>>();
        return await handler.Handle(request, ct);
    }
}

