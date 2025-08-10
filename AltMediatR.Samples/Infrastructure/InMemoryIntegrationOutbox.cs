using AltMediatR.Core.Abstractions;
using System.Collections.Concurrent;

namespace AltMediatR.Samples.Infrastructure
{
    public sealed class InMemoryIntegrationOutbox : IIntegrationOutbox
    {
        private static readonly ConcurrentQueue<IIntegrationEvent> _backlog = new();

        public Task SaveAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
        {
            _backlog.Enqueue(@event);
            Console.WriteLine($"[Outbox] Stored integration event {@event.GetType().Name} for later publish.");
            return Task.CompletedTask;
        }

        // Helper for demo to drain outbox
        public static IEnumerable<IIntegrationEvent> Drain()
        {
            while (_backlog.TryDequeue(out var evt))
                yield return evt;
        }
    }
}
