using System.Collections.Concurrent;
using AltMediatR.DDD.Abstractions;

namespace AltMediatR.DDD.Infrastructure
{
    /// <summary>
    /// Default scoped implementation of IDomainEventQueue.
    /// </summary>
    internal sealed class DomainEventQueue : IDomainEventQueue
    {
        private readonly ConcurrentQueue<IDomainEvent> _queue = new();

        public void Enqueue(IDomainEvent @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            _queue.Enqueue(@event);
        }

        public IReadOnlyCollection<IDomainEvent> DequeueAll()
        {
            var list = new List<IDomainEvent>();
            while (_queue.TryDequeue(out var evt)) list.Add(evt);
            return list;
        }
    }
}
