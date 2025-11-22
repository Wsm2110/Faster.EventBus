# ⚡ Faster.EventBus — Ultra-High-Performance In-Process Command & Event Dispatcher

A **near-zero-allocation**, **pipeline-optimized** alternative to **MediatR** designed for real-time .NET systems.

Faster.EventBus dispatches commands and publishes events using **compiled pipeline delegates**, avoiding reflection, boxing, and runtime allocations.  
Built for extremely high throughput, predictable tail latency, and event processing inside a single .NET process.

---

## ✨ Key Features
- ⚡ Fastest .NET mediator-style system
- 🧠 No reflection or boxing in hot path
- 🍃 Zero allocation `ValueTask<T>` pipelines
- 🧵 Middleware-style pipeline behaviors
- 📣 Publish/subscribe event fan-out
- 🏗 **Automatically registers all command & event handlers as Singletons**
- 💉 DI integrated
- 🧪 Benchmark-proven faster than MediatR

---

## 📦 Installation
```csharp
services.AddEventBus();
```

## 📌 Define a Command
```csharp
public record GetUserNameCommand(int UserId) : ICommand<Result<string>>;
```

## 🛠 Create a Command Handler
```csharp
public class GetUserNameCommandHandler :
    ICommandHandler<GetUserNameCommand, Result<string>>
{
    public ValueTask<Result<string>> Handle(GetUserNameCommand command, CancellationToken ct)
    {
        return ValueTask.FromResult(Result<string>.Success($"User-{command.UserId}"));
    }
}
```

---

## 🧩 Automatic Registration (DI)
Calling `services.AddEventBus()` automatically:

| Type | Lifetime |
|-------|-----------|
| `ICommandHandler<TCommand,TResponse>` | Singleton |
| `IEventHandler<TEvent>` | Singleton |
| `IPipelineBehavior<TCommand,TResponse>` | Transient |

```csharp
services.AddEventBus(); // Auto-detects DI handlers and behaviors
```

---

## 🚀 Send a Command
```csharp
var result = await bus.Send(new GetUserNameCommand(42));
Console.WriteLine(result.Value);
```

---

## 🔧 Pipeline Behaviors Example

### Logging Behavior
```csharp
public class LoggingBehavior<TCommand, TResponse> : IPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TCommand command,
        CommandBehaviorDelegate<TResponse> next,
        CancellationToken ct)
    {
        Console.WriteLine("Before");
        var result = await next();
        Console.WriteLine("After");
        return result;
    }
}
```

### Validation Behavior
```csharp
public class ValidationBehavior<TCommand, TResponse> : IPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TCommand command,
        CommandBehaviorDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (command is IValidatable v && !v.IsValid(out var errors))
            throw new ValidationException(errors);

        return await next();
    }
}
```

### Register behaviors
```csharp
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

Execution Chain:
```
Logging → Validation → Handler
```

---

## 📣 Publish / Subscribe Events
```csharp
public record UserCreatedEvent(int UserId) : IEvent;

public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    public ValueTask Handle(UserCreatedEvent evt, CancellationToken ct)
    {
        Console.WriteLine($"User created: {evt.UserId}");
        return ValueTask.CompletedTask;
    }
}
```

### Registration
```csharp
services.AddSingleton<IEventHandler<UserCreatedEvent>, UserCreatedEventHandler>();
```

### Publish
```csharp
await bus.Publish(new UserCreatedEvent(10));
```

# 🧠 Use Cases (`IUseCaseHandler`) — Why and How We Use Them

## 🎯 What is a Use Case?

A **Use Case** represents a high-level business workflow initiated by external interaction (UI, API, event, scheduler).  
It coordinates multiple operations and executes **business orchestration**, while `ICommandHandler<TCommand,TResponse>` executes a **single atomic action**.

UseCases form the **public entry boundary** for each feature/module in the modular monolith.  
Everything inside a module (commands, handlers, domain, repositories) is private and cannot be accessed from other modules.

---

## ❓ Why do we need UseCases?

Without UseCases, systems often fall into the anti-pattern where handlers call handlers:


### ❌ Problems with handler-chaining

| Problem | Description |
|--------|------------|
| Hidden workflow | Flow of business rules is scattered and buried inside handlers |
| Tight coupling | Module A ends up depending on internal code in Module B |
| Hard to test | Requires mocking long dependency call chains |
| Hard to reason about | No single place that reveals full business process |
| Architecture rot | Evolves into spaghetti and breaks modular boundaries |

---

## ✔ Correct approach: orchestrate inside a UseCase


### 👍 Benefits

| Benefit | Description |
|---------|-------------|
| Visible business flow | Easily readable orchestration logic |
| Clean module boundaries | Modules communicate only through use cases |
| Atomic handlers | Each handler does one thing only |
| Easy testing | No deep mock chain dependencies |
| Replaceable internals | Modules can refactor commands without breaking consumers |

---

## 📦 The `IUseCaseHandler` Interface

UseCases **do not** support pipeline behaviors or middleware.  
They are intentionally lean, explicit, and synchronous.

```csharp
public interface IUseCaseHandler<TRequest, TResponse>
{
    ValueTask<TResponse> Handle(TRequest request, CancellationToken ct = default);
}
```

## Example UseCase
```csharp
public sealed class CheckoutUseCase :
    IUseCaseHandler<CheckoutRequest, Result>
{
    private readonly ICommandDispatcher _dispatcher;

    public CheckoutUseCase(ICommandDispatcher dispatcher)
        => _dispatcher = dispatcher;

    public async ValueTask<Result> Handle(CheckoutRequest request, CancellationToken ct)
    {
        var placed = await _dispatcher.Send(new PlaceOrderCommand(request.OrderId), ct);
        if (placed.IsFailure) return placed;

        var paid = await _dispatcher.Send(new ChargePaymentCommand(request.OrderId), ct);
        if (paid.IsFailure) return paid;

        return await _dispatcher.Send(new ShipOrderCommand(request.OrderId), ct);
    }
}
```

---

## 🧠 Why Event Handlers Must Be Singletons
- Prevent duplicate fan-out
- Avoid re-subscription cost
- Avoid allocation spikes
- Maintain subscription lifetime consistency

✔ Correct lifetime:
```csharp
services.AddSingleton<IEventHandler<UserCreatedEvent>, UserCreatedEventHandler>();
```

---

## 🥇 Benchmark Results vs MediatR

| Method | Calls | Mean (ns) | Ratio | Alloc | Alloc Ratio |
|--------|--------|------------:|-------:|-------:|------------:|
| Faster.EventBus | 1 | 68.37 | 1.00x | 128 B | 1.00x |
| Mediatr | 1 | 127.56 | 1.87x | 504 B | 3.94x |
| Faster.EventBus | 100 | 6,190 | 1.00x | 12 KB | 1.00x |
| Mediatr | 100 | 11,584 | 1.87 | 50 KB | 3.94x |

🔥 **~2× faster & ~4× less memory than MediatR**

---

## ❤️ Summary
Fast. Lightweight. Production-ready.  
If performance matters — use **Faster.EventBus**.