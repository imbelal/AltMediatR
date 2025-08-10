using System.Collections.Generic;

namespace AltMediatR.Core.Abstractions
{
    /// <summary>
    /// Scoped queue to collect integration events during command execution for later publishing.
    /// </summary>
    public interface IIntegrationEventQueue
    {
        void Enqueue(IIntegrationEvent @event);
        IReadOnlyCollection<IIntegrationEvent> DequeueAll();
    }
}
