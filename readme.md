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
