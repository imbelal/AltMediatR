using System.Collections.Generic;

namespace AltMediatR.DDD.Domain
{
    using AltMediatR.DDD.Abstractions;

    public abstract class AggregateRootBase
    {
        private readonly List<IDomainEvent> _domainEvents = new();
        private readonly List<IIntegrationEvent> _integrationEvents = new();

        protected void RaiseDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);
        protected void RaiseIntegrationEvent(IIntegrationEvent @event) => _integrationEvents.Add(@event);

        internal IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        internal IReadOnlyCollection<IIntegrationEvent> IntegrationEvents => _integrationEvents.AsReadOnly();

        internal void ClearEvents()
        {
            _domainEvents.Clear();
            _integrationEvents.Clear();
        }
    }
}
