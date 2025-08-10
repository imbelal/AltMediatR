using System.Collections.Concurrent;
using AltMediatR.DDD.Abstractions;

namespace AltMediatR.DDD.Infrastructure
{
    public sealed class InMemoryOutboxStore : IOutboxStore
    {
        private readonly ConcurrentDictionary<Guid, IIntegrationEvent> _pending = new();

        public Task SaveAsync(IIntegrationEvent @event, CancellationToken ct)
        {
            _pending[@event.Id] = @event;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<IIntegrationEvent>> GetPendingEventsAsync(CancellationToken ct)
            => Task.FromResult<IEnumerable<IIntegrationEvent>>(_pending.Values.ToArray());

        public Task MarkAsPublishedAsync(Guid eventId, CancellationToken ct)
        {
            _pending.TryRemove(eventId, out _);
            return Task.CompletedTask;
        }
    }
}
