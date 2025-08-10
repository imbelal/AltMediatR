namespace AltMediatR.DDD.Abstractions
{
    public interface IIntegrationEventQueue
    {
        void Enqueue(IIntegrationEvent @event);
        IReadOnlyCollection<IIntegrationEvent> DequeueAll();
    }
}
