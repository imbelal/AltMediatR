namespace AltMediatR.DDD.Abstractions
{
    public interface IDomainEventQueue
    {
        void Enqueue(IDomainEvent @event);
        IReadOnlyCollection<IDomainEvent> DequeueAll();
    }
}
