using AltMediatR.DDD.Abstractions;

namespace AltMediatR.DDD.Infrastructure
{
    /// <summary>
    /// Simple in-memory outbox processor that drains IOutboxStore periodically when hosted service runs,
    /// or can be invoked ad-hoc via ProcessOnceAsync.
    /// </summary>
    public sealed class InMemoryOutboxProcessor : IOutboxProcessor
    {
        private readonly IOutboxStore _outboxStore;
        private readonly IIntegrationEventPublisher _publisher;

        public InMemoryOutboxProcessor(IOutboxStore outboxStore, IIntegrationEventPublisher publisher)
        {
            _outboxStore = outboxStore;
            _publisher = publisher;
        }

        public async Task ProcessOnceAsync(CancellationToken cancellationToken = default)
        {
            var pending = await _outboxStore.GetPendingEventsAsync(cancellationToken).ConfigureAwait(false);

            foreach (var evt in pending)
            {
                try
                {
                    await _publisher.PublishAsync(evt, cancellationToken).ConfigureAwait(false);
                    await _outboxStore.MarkAsPublishedAsync(evt.Id, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Leave event in outbox for retry on next cycle
                }
            }
        }
    }
}
