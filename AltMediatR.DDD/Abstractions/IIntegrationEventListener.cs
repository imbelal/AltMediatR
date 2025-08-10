namespace AltMediatR.DDD.Abstractions
{
    /// <summary>
    /// Abstraction for subscribing to external integration events (e.g., from a message bus)
    /// and routing them to in-process handlers.
    /// Implementations can resolve IIntegrationEventHandler<TEvent> and invoke them on message receipt.
    /// Typically wrapped or orchestrated by an IInboundMessageProcessor implementation.
    /// </summary>
    public interface IIntegrationEventListener
    {
        Task SubscribeAsync<TEvent>(CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent;

        Task UnsubscribeAsync<TEvent>(CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent;

        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
    }
}
