using System.Collections.Concurrent;
using AltMediatR.DDD.Abstractions;

namespace AltMediatR.DDD.Infrastructure
{
    /// <summary>
    /// Default scoped implementation of IIntegrationEventQueue.
    /// </summary>
    internal sealed class IntegrationEventQueue : IIntegrationEventQueue
    {
        private readonly ConcurrentQueue<IIntegrationEvent> _queue = new();

        public void Enqueue(IIntegrationEvent @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            _queue.Enqueue(@event);
        }

        public IReadOnlyCollection<IIntegrationEvent> DequeueAll()
        {
            var list = new List<IIntegrationEvent>();
            while (_queue.TryDequeue(out var evt)) list.Add(evt);
            return list;
        }
    }
}
