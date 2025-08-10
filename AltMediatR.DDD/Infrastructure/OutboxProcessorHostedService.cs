using Microsoft.Extensions.Hosting;

namespace AltMediatR.DDD.Infrastructure
{
    /// <summary>
    /// Background service that periodically runs the configured outbox processor.
    /// </summary>
    public sealed class OutboxProcessorHostedService : IHostedService, IDisposable
    {
        private readonly Abstractions.IOutboxProcessor _processor;
        private readonly TimeSpan _interval;
        private CancellationTokenSource? _cts;
        private Task? _loop;

        public OutboxProcessorHostedService(Abstractions.IOutboxProcessor processor, TimeSpan interval)
        {
            _processor = processor;
            _interval = interval <= TimeSpan.Zero ? TimeSpan.FromSeconds(10) : interval;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _loop = Task.Run(() => RunLoopAsync(_cts.Token), _cts.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                try { if (_loop != null) await _loop.ConfigureAwait(false); } catch { }
            }
        }

        private async Task RunLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await _processor.ProcessOnceAsync(ct).ConfigureAwait(false);
                }
                catch
                {
                    // swallow; next tick will retry
                }

                try { await Task.Delay(_interval, ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
