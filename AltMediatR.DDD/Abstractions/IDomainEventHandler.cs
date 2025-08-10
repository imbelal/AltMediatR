using AltMediatR.Core.Abstractions;

namespace AltMediatR.DDD.Abstractions
{
    /// <summary>
    /// Domain event handler abstraction exposed from DDD package.
    /// Inherits the core notification handler so handlers still participate in mediator PublishAsync.
    /// </summary>
    public interface IDomainEventHandler<in TEvent> : INotificationHandler<TEvent>
        where TEvent : IDomainEvent
    {
    }
}
