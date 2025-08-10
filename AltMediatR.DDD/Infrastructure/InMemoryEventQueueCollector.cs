using AltMediatR.DDD.Abstractions;
using AltMediatR.DDD.Domain;

namespace AltMediatR.DDD.Infrastructure
{
    public sealed class InMemoryEventQueueCollector : IEventQueueCollector
    {
        private readonly List<AggregateRootBase> _tracked = new();

        // Non-interface helper for manual registration in non-ORM flows (samples/tests)
        public void Register(AggregateRootBase aggregate)
        {
            if (aggregate != null)
                _tracked.Add(aggregate);
        }

        public IEnumerable<IDomainEvent> CollectDomainEvents()
            => _tracked.SelectMany(a => a.DomainEvents).ToArray();

        public IEnumerable<IIntegrationEvent> CollectIntegrationEvents()
            => _tracked.SelectMany(a => a.IntegrationEvents).ToArray();

        public void ClearEvents()
        {
            foreach (var a in _tracked) a.ClearEvents();
            _tracked.Clear();
        }
    }
}
