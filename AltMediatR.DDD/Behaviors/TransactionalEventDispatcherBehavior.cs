using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Deligates;
using AltMediatR.DDD.Abstractions;

namespace AltMediatR.DDD.Behaviors
{
    public enum DispatchOrder { DomainFirst, IntegrationFirst }

    public sealed class DddMediatorOptions
    {
        public DispatchOrder DispatchOrder { get; set; } = DispatchOrder.DomainFirst;
        public bool ParallelDispatch { get; set; } = false;
    }

    /// <summary>
    /// Wraps request handling in a DB transaction; configurable dispatch of domain and integration events collected from aggregates.
    /// On publish failure, stores integration events in outbox store and still commits the transaction.
    /// </summary>
    public sealed class TransactionalEventDispatcherBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ITransactionManager _txManager;
        private readonly IMediator _mediator;
        private readonly IIntegrationEventPublisher? _integrationPublisher;
        private readonly IOutboxStore? _outboxStore;  // pluggable outbox store
        private readonly IEventQueueCollector _collector; // required (AggregateRootBase flows)
        private readonly DddMediatorOptions _options;

        public TransactionalEventDispatcherBehavior(
            ITransactionManager txManager,
            IMediator mediator,
            IEventQueueCollector collector,
            IIntegrationEventPublisher? integrationPublisher = null,
            IOutboxStore? outboxStore = null,
            DddMediatorOptions? options = null)
        {
            _txManager = txManager;
            _mediator = mediator;
            _collector = collector ?? throw new ArgumentNullException(nameof(collector));
            _integrationPublisher = integrationPublisher;
            _outboxStore = outboxStore;
            _options = options ?? new DddMediatorOptions();
        }

        public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            await using var scope = await _txManager.BeginAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var response = await next().ConfigureAwait(false);

                var domainEvents = _collector.CollectDomainEvents().ToArray();
                var integrationEvents = _collector.CollectIntegrationEvents().ToArray();

                if (_options.ParallelDispatch)
                {
                    var tasks = _options.DispatchOrder == DispatchOrder.DomainFirst
                        ? new[] { DispatchDomainAsync(domainEvents, cancellationToken), DispatchIntegrationAsync(integrationEvents, cancellationToken) }
                        : new[] { DispatchIntegrationAsync(integrationEvents, cancellationToken), DispatchDomainAsync(domainEvents, cancellationToken) };
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                else
                {
                    if (_options.DispatchOrder == DispatchOrder.DomainFirst)
                    {
                        await DispatchDomainAsync(domainEvents, cancellationToken).ConfigureAwait(false);
                        await DispatchIntegrationAsync(integrationEvents, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await DispatchIntegrationAsync(integrationEvents, cancellationToken).ConfigureAwait(false);
                        await DispatchDomainAsync(domainEvents, cancellationToken).ConfigureAwait(false);
                    }
                }

                _collector.ClearEvents();
                await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
                return response;
            }
            catch
            {
                try { await scope.RollbackAsync(cancellationToken).ConfigureAwait(false); } catch { }
                _collector.ClearEvents();
                throw;
            }
        }

        private async Task DispatchDomainAsync(IEnumerable<IDomainEvent> events, CancellationToken ct)
        {
            foreach (var domainEvent in events)
                await _mediator.PublishAsync(domainEvent, ct).ConfigureAwait(false);
        }

        private async Task DispatchIntegrationAsync(IEnumerable<IIntegrationEvent> events, CancellationToken ct)
        {
            var list = events as ICollection<IIntegrationEvent> ?? events.ToList();
            if (list.Count == 0) return;

            if (_integrationPublisher == null && _outboxStore == null)
                throw new InvalidOperationException("No integration publisher or outbox store configured.");

            foreach (var evt in list)
            {
                var published = false;
                if (_integrationPublisher != null)
                {
                    try
                    {
                        await _integrationPublisher.PublishAsync(evt, ct).ConfigureAwait(false);
                        published = true;
                    }
                    catch
                    {
                        // fall through to outbox store
                    }
                }

                if (!published)
                {
                    if (_outboxStore != null)
                    {
                        await _outboxStore.SaveAsync(evt, ct).ConfigureAwait(false);
                        published = true;
                    }
                }

                if (!published)
                    throw new InvalidOperationException("Failed to publish integration event and no outbox store available.");
            }
        }
    }
}
