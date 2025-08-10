using System.Collections.Generic;

namespace AltMediatR.DDD.Abstractions
{
    public interface IOutboxStore
    {
        Task SaveAsync(IIntegrationEvent @event, CancellationToken ct);
        Task<IEnumerable<IIntegrationEvent>> GetPendingEventsAsync(CancellationToken ct);
        Task MarkAsPublishedAsync(Guid eventId, CancellationToken ct);
    }
}
