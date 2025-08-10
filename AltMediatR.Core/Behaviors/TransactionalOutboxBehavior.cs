using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Deligates;

namespace AltMediatR.Core.Behaviors
{
    /// <summary>
    /// Wraps request handling in a DB transaction; dispatches domain events before commit;
    /// attempts to publish integration events; on publish failure, stores them in outbox and still commits the transaction.
    /// Rolls back on unhandled failure.
    /// </summary>
    public sealed class TransactionalOutboxBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ITransactionManager _txManager;
        private readonly IMediator _mediator;
        private readonly IDomainEventQueue _domainQueue;
        private readonly IIntegrationEventQueue _integrationQueue;
        private readonly IIntegrationEventPublisher? _integrationPublisher;
        private readonly IIntegrationOutbox? _outbox;

        public TransactionalOutboxBehavior(
            ITransactionManager txManager,
            IMediator mediator,
            IDomainEventQueue domainQueue,
            IIntegrationEventQueue integrationQueue,
            IIntegrationEventPublisher? integrationPublisher = null,
            IIntegrationOutbox? outbox = null)
        {
            _txManager = txManager;
            _mediator = mediator;
            _domainQueue = domainQueue;
            _integrationQueue = integrationQueue;
            _integrationPublisher = integrationPublisher;
            _outbox = outbox;
        }

        public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            await using var scope = await _txManager.BeginAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Execute handler within transaction
                var response = await next().ConfigureAwait(false);

                // 1) Dispatch domain events in-process (participates in same transaction)
                foreach (var domainEvent in _domainQueue.DequeueAll())
                {
                    await _mediator.PublishDomainEventAsync(domainEvent, cancellationToken).ConfigureAwait(false);
                }

                // 2) Try publishing integration events; on failure, save to outbox
                var integrationEvents = _integrationQueue.DequeueAll();
                if (_integrationPublisher != null && integrationEvents.Count > 0)
                {
                    foreach (var evt in integrationEvents)
                    {
                        try
                        {
                            await _integrationPublisher.PublishAsync(evt, cancellationToken).ConfigureAwait(false);
                        }
                        catch
                        {
                            if (_outbox == null) throw; // no outbox configured, bubble up
                            await _outbox.SaveAsync(evt, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                else if (_outbox != null)
                {
                    foreach (var evt in integrationEvents)
                    {
                        await _outbox.SaveAsync(evt, cancellationToken).ConfigureAwait(false);
                    }
                }

                // 3) Commit transaction
                await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
                return response;
            }
            catch
            {
                // Roll back on failure and clear queues to avoid leaks
                try { await scope.RollbackAsync(cancellationToken).ConfigureAwait(false); } catch { }
                _ = _domainQueue.DequeueAll();
                _ = _integrationQueue.DequeueAll();
                throw;
            }
        }
    }
}
