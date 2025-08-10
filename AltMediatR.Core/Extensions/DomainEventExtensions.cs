using AltMediatR.Core.Abstractions;

namespace AltMediatR.Core.Extensions
{
    public static class DomainEventExtensions
    {
        /// <summary>
        /// Enqueue a domain event for dispatch after the current command completes.
        /// Typical usage: inside a handler after state changes.
        /// </summary>
        public static void AddDomainEvent(this IDomainEventQueue queue, IDomainEvent @event)
        {
            queue.Enqueue(@event);
        }
    }
}
