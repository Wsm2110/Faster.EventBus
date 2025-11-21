using Faster.EventBus.Contracts;

namespace Faster.EventBus
{
    public interface IEventDispatcher
    {
        void Publish<TEvent>(TEvent evt, CancellationToken ct = default) where TEvent : IEvent;
        void RegisterCommandHandler<THandler>();
        ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default);
        void Subscribe<TEvent>() where TEvent : IEvent;
        IEventDispatcher Initialize();
    }
}