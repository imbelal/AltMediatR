using System.Collections.Concurrent;
using AltMediatR.Core.Abstractions;

namespace AltMediatR.Core.Infrastructure
{
    /// <summary>
    /// Default scoped implementation of IDomainEventQueue.
    /// </summary>
    internal sealed class DomainEventQueue : IDomainEventQueue
    {
        private readonly ConcurrentQueue<IDomainEvent> _queue = new();

        public void Enqueue(IDomainEvent @event)
        {
            if (@event == null) return;
            _queue.Enqueue(@event);
        }

        public IReadOnlyCollection<IDomainEvent> DequeueAll()
        {
            var list = new List<IDomainEvent>();
            while (_queue.TryDequeue(out var e))
            {
                list.Add(e);
            }
            return list;
        }
    }
}
