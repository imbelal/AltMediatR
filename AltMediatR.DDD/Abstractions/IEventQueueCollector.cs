using System.Collections.Generic;

namespace AltMediatR.DDD.Abstractions
{
    public interface IEventQueueCollector
    {
        IEnumerable<IDomainEvent> CollectDomainEvents();
        IEnumerable<IIntegrationEvent> CollectIntegrationEvents();
        void ClearEvents();
    }
}
