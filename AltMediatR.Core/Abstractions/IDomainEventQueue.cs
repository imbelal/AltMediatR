using System.Collections.Generic;

namespace AltMediatR.Core.Abstractions
{
    /// <summary>
    /// Scoped queue to collect domain/integration events during a command's execution.
    /// A pipeline behavior will dispatch and then clear them after the handler completes.
    /// </summary>
    public interface IDomainEventQueue
    {
        void Enqueue(IDomainEvent @event);
        IReadOnlyCollection<IDomainEvent> DequeueAll();
    }
}
