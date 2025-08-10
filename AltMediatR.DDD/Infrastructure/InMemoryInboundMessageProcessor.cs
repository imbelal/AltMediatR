using System.Collections.Concurrent;
using AltMediatR.DDD.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AltMediatR.DDD.Infrastructure
{
    /// <summary>
    /// In-memory inbound message processor. Provides an in-process queue to enqueue integration events
    /// and dispatches them to registered IIntegrationEventHandler<TEvent> handlers.
    /// Useful for local dev/tests without a real broker.
    /// </summary>
    public sealed class InMemoryInboundMessageProcessor : IInboundMessageProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentQueue<IIntegrationEvent> _queue = new();
        private readonly SemaphoreSlim _signal = new(0);
        private CancellationTokenSource? _cts;
        private Task? _loop;

        public InMemoryInboundMessageProcessor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task EnqueueAsync(IIntegrationEvent message, CancellationToken cancellationToken = default)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            _queue.Enqueue(message);
            _signal.Release();
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_loop != null)
                return Task.CompletedTask;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _loop = Task.Run(() => RunLoopAsync(_cts.Token), _cts.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_cts == null) return;
            _cts.Cancel();
            try { if (_loop != null) await _loop.ConfigureAwait(false); } catch { }
        }

        private async Task RunLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (!_queue.TryDequeue(out var msg))
                    {
                        await _signal.WaitAsync(ct).ConfigureAwait(false);
                        continue;
                    }

                    await DispatchAsync(msg, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch
                {
                    // swallow and continue loop
                }
            }
        }

        private async Task DispatchAsync(IIntegrationEvent message, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;

            var handlerInterface = typeof(IIntegrationEventHandler<>).MakeGenericType(message.GetType());
            var handlers = sp.GetServices(handlerInterface);

            foreach (var handler in handlers)
            {
                var method = handlerInterface.GetMethod("HandleAsync");
                if (method != null)
                {
                    var task = (Task?)method.Invoke(handler, new object[] { message, ct });
                    if (task != null) await task.ConfigureAwait(false);
                }
            }
        }
    }
}
