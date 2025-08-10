namespace AltMediatR.Core.Abstractions
{
    public interface IMediator
    {
        /// <summary>
        /// Sends a request that expects a response to a single handler.
        /// </summary>
        Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a request that has no response payload to a single handler.
        /// </summary>
        Task SendAsync(IRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a notification to all registered handlers.
        /// </summary>
        /// <typeparam name="TNotification"></typeparam>
        /// <param name="notification"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification;

        /// <summary>
        /// Publish an in-process domain event via notification handlers.
        /// </summary>
        Task PublishDomainEventAsync<TDomainEvent>(TDomainEvent @event, CancellationToken cancellationToken = default)
            where TDomainEvent : IDomainEvent;

        /// <summary>
        /// Publish an integration event via the configured transport publisher.
        /// </summary>
        Task PublishIntegrationEventAsync<TIntegrationEvent>(TIntegrationEvent @event, CancellationToken cancellationToken = default)
            where TIntegrationEvent : IIntegrationEvent;
    }
}
