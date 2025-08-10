using System.Collections.Concurrent;
using AltMediatR.Core.Abstractions;

namespace AltMediatR.Core.Infrastructure
{
    /// <summary>
    /// Default scoped implementation of IIntegrationEventQueue.
    /// </summary>
    internal sealed class IntegrationEventQueue : IIntegrationEventQueue
    {
        private readonly ConcurrentQueue<IIntegrationEvent> _queue = new();

        public void Enqueue(IIntegrationEvent @event)
        {
            if (@event == null) return;
            _queue.Enqueue(@event);
        }

        public IReadOnlyCollection<IIntegrationEvent> DequeueAll()
        {
            var list = new List<IIntegrationEvent>();
            while (_queue.TryDequeue(out var e))
            {
                list.Add(e);
            }
            return list;
        }
    }
}
