# Faster.EventBus – High-Performance In-Process Command & Event Dispatcher

A faster, lower-allocation alternative to **MediatR** for .NET real-time workloads.

Faster.EventBus is an ultra-fast mediator/event bus for .NET.  
It dispatches commands with `Result<T>` responses and publishes events using compiled pipeline delegates, avoiding runtime reflection and minimizing allocations.

It is ideal for high-frequency request handling, simulations, UI frameworks, real-time systems, plugin architectures, and performance-critical backends.

---

## ✨ Key Features

- ✔ Near-zero overhead runtime
- ✔ No reflection after startup
- ✔ No boxing / extremely low memory allocation
- ✔ `ValueTask<T>` pipelines
- ✔ Middleware-style pipeline behaviors
- ✔ Command/query request-response pattern
- ✔ Publish/subscribe events
- ✔ Fully DI-integrated via `IServiceProvider`
- ✔ Benchmark-proven micro-latency advantage over **MediatR**

---

## 📦 Installation
```csharp
services.AddEventBus(options =>
{
    options.AutoRegister = true; // automatically scans assemblies for handlers
});
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

## 🧩 Register Services
```csharp
var services = new ServiceCollection();

services.AddEventBus(options => options.AutoRegister = true);
services.AddTransient<ICommandHandler<GetUserNameCommand, Result<string>>, GetUserNameCommandHandler>();

var provider = services.BuildServiceProvider();
var bus = provider.GetRequiredService<EventDispatcher>();
```

## 🚀 Send a Command
```csharp
var result = await bus.Send(new GetUserNameCommand(42));

if (result.IsSuccess)
    Console.WriteLine(result.Value); // Output: User-42
```

## 🔧 Pipeline Behavior Example
```csharp
public class LoggingBehavior : ICommandPipelineBehavior<GetUserNameCommand, Result<string>>
{
    public async ValueTask<Result<string>> Handle(
        GetUserNameCommand cmd, CancellationToken ct, CommandHandlerDelegate<Result<string>> next)
    {
        Console.WriteLine("Before");
        var result = await next();
        Console.WriteLine("After");
        return result;
    }
}
```

## Register Behavior
```csharp
services.AddSingleton<ICommandPipelineBehavior<GetUserNameCommand, Result<string>>, LoggingBehavior>();
```

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

## 🧠 Why `IEventHandler<TEvent>` Must Be Registered as **Singletons**

Event handlers represent **long-lived subscribers** to a stream of events.  
A handler expresses an intention to receive **all occurrences** of a specific event during application execution.

### ❌ Transient handlers do **not** make sense

Transient services are short-lived and created per request or operation.  
Attempting to subscribe a transient handler leads to:

- 🔁 Re-subscribing every time the handler is constructed
- 📈 Growing subscription lists
- 🗑 Memory leaks and duplicated execution
- 🤯 Hard-to-reason dependency lifecycle issues

### ✔ Correct model

Event handlers must be registered as **Singletons**, while their dependencies may be transient or scoped.


## Use in application
```csharp
bus.SubscribeEvent<UserCreatedEvent>();
await bus.PublishEvent(new UserCreatedEvent(10));
```
## 🥇 Benchmark Results vs MediatR

**BenchmarkDotNet v0.15.6 • .NET 10 • 12-Core i5-12500H • Windows 11**

| Method                  | Length | Mean        | Ratio | Alloc  | Alloc Ratio |
|-------------------------|--------|------------:|------:|-------:|------------:|
| Faster_EventBus_Result  | 1      | 98.57 ns    | 1.00x | 168 B  | 1.00x       |
| MediatR_Result          | 1      | 123.12 ns   | 1.25x | 504 B  | 3.00x       |
| Faster_EventBus_Result  | 100    | 8,672 ns    | 1.00x | 16 KB  | 1.00x       |
| MediatR_Result          | 100    | 11,688 ns   | 1.35x | 50 KB  | 3.00x       |
| Faster_EventBus_Result  | 1000   | 87,482 ns   | 1.00x | 168 KB | 1.00x       |
| MediatR_Result          | 1000   | 118,251 ns  | 1.35x | 504 KB | 3.00x       |

### 🔥 Key takeaway

**Faster.EventBus is 1.25–1.35x faster and uses 3x less memory.**

---

## ❤️ Why Use It?

| Need                                | Solution |
|-------------------------------------|----------|
| High-volume real-time commands      | ✔        |
| Minimal GC pressure                 | ✔        |
| No reflection overhead              | ✔        |
| Mid-pipeline customization          | ✔        |
| Faster alternative to MediatR       | ✔        |

---

## 🙌 Final Notes

Fast, simple, reliable.  
Perfect when performance matters.
