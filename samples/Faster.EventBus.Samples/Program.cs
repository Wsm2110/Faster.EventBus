using Faster.EventBus;
using Faster.EventBus.Extensions;
using Faster.EventBus.Samples.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace EventBus.ConsoleSample
{
    internal class Program
    {
        /// <summary>
        /// Simple console sample that demonstrates using the in-process bus.
        /// </summary>
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting EventBus console sample...");

            // 1. Setup DI container
            var services = new ServiceCollection();
            services.AddEventBus(options => options.AutoScan = true);
                     
            var provider = services.BuildServiceProvider();
            var bus = provider.GetRequiredService<EventDispatcher>();
      
            // 3. send first command
            var result = await bus.Send(new SendLog("severity:fatal,something terrible happend"));
            Console.WriteLine($"CreateUser result: {result}");

            // 4. Send second command
            var command = new CreateUser("Alice");
            result = await bus.Send(command);
            Console.WriteLine($"CreateUser result: {result}");

            // 4. Publish an event
            bus.PublishEvent(new UserCreated("Alice"));

            Console.WriteLine("Done. Press any key to exit.");
            Console.ReadKey();
        }
    }
}