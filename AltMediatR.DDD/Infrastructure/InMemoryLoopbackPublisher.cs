using AltMediatR.DDD.Abstractions;

namespace AltMediatR.DDD.Infrastructure
{
    /// <summary>
    /// Integration event publisher that loops back into the in-memory inbound processor.
    /// Useful for demos: published integration events are immediately enqueued for in-process handlers.
    /// </summary>
    public sealed class InMemoryLoopbackPublisher : IIntegrationEventPublisher
    {
        private readonly InMemoryInboundMessageProcessor _inbound;
        public InMemoryLoopbackPublisher(InMemoryInboundMessageProcessor inbound) => _inbound = inbound;

        public Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
            => _inbound.EnqueueAsync(@event, cancellationToken);
    }
}
