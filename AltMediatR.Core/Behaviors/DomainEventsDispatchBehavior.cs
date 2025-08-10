using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Deligates;

namespace AltMediatR.Core.Behaviors
{
    /// <summary>
    /// After the handler completes successfully, dispatches any queued domain events using in-process notification handlers,
    /// then publishes any queued integration events via IIntegrationEventPublisher.
    /// </summary>
    public sealed class DomainEventsDispatchBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IMediator _mediator;
        private readonly IDomainEventQueue _domainQueue;
        private readonly IIntegrationEventQueue _integrationQueue;
        private readonly IIntegrationEventPublisher? _integrationPublisher;

        public DomainEventsDispatchBehavior(
            IMediator mediator,
            IDomainEventQueue domainQueue,
            IIntegrationEventQueue integrationQueue,
            IIntegrationEventPublisher? integrationPublisher = null)
        {
            _mediator = mediator;
            _domainQueue = domainQueue;
            _integrationQueue = integrationQueue;
            _integrationPublisher = integrationPublisher;
        }

        public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            var success = false;
            try
            {
                var response = await next().ConfigureAwait(false);

                // Dispatch domain events (in-process) within the same scope/transaction
                foreach (var domainEvent in _domainQueue.DequeueAll())
                {
                    await _mediator.PublishDomainEventAsync(domainEvent, cancellationToken).ConfigureAwait(false);
                }

                // Publish integration events (out-of-process)
                if (_integrationPublisher != null)
                {
                    foreach (var integrationEvent in _integrationQueue.DequeueAll())
                    {
                        await _integrationPublisher.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
                    }
                }

                success = true;
                return response;
            }
            finally
            {
                // If the handler/transaction failed, clear queued events so they don't leak into a subsequent call within the same scope.
                if (!success)
                {
                    _ = _domainQueue.DequeueAll();
                    _ = _integrationQueue.DequeueAll();
                }
            }
        }
    }
}
