namespace AltMediatR.DDD.Abstractions
{
    public interface IIntegrationEventPublisher
    {
        Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default);
    }
}
