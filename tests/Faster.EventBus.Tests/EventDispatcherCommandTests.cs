using Faster.EventBus.Contracts;
using Faster.EventBus.Extensions;
using Faster.EventBus.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Faster.EventBus.Tests
{
    public class EventDispatcherCommandTests
    {
        [Fact]
        public async Task SendCommand_ShouldReturnSuccess()
        {
            // Arrange
            var services = new ServiceCollection();

            services.AddEventBus();

            services.AddSingleton<ICommandHandler<AddValueCommand, Result>, AddValueCommandHandler>();
            services.AddSingleton<EventBus>();

            var provider = services.BuildServiceProvider();
            var bus = provider.GetRequiredService<IEventBus>();

            // Act
            var result = await bus.Send(new AddValueCommand(10));

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task SendCommand_ShouldReturnSuccess_AutoRegister()
        {
            // Arrange
            var services = new ServiceCollection();

            services.AddEventBus();

            // Required for auto register, leftovers from different tests...
            services.AddSingleton<IList<string>, List<string>>();
            services.AddSingleton<MyDependency>();

            var provider = services.BuildServiceProvider();
            var bus = provider.GetRequiredService<IEventBus>();

            // Act
            var result = await bus.Send(new AddValueCommand(10));

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task SendCommand_ShouldInvokeHandlerLogic()
        {
            var handler = new AddValueCommandHandler();

            var services = new ServiceCollection();
            services.AddEventBus();

            services.AddSingleton<ICommandHandler<AddValueCommand, Result>>(handler);
            services.AddSingleton<EventBus>();

            var provider = services.BuildServiceProvider();
            var bus = provider.GetRequiredService<IEventBus>();
                     
            await bus.Send(new AddValueCommand(30));

            Assert.Equal(30, handler.LastValue);
        }
    }

    public record AddValueCommand(int Value) : ICommand<Result>;

    public class AddValueCommandHandler : ICommandHandler<AddValueCommand, Result>
    {
        public int LastValue { get; private set; }

        public ValueTask<Result> Handle(AddValueCommand cmd, CancellationToken ct)
        {
            LastValue = cmd.Value;
            return ValueTask.FromResult(Result.Success());
        }
    }
}
