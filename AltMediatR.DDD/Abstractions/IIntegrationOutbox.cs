namespace AltMediatR.DDD.Abstractions
{
    /// <summary>
    /// Outbox store for integration events to guarantee eventual delivery when publish fails.
    /// </summary>
    public interface IIntegrationOutbox
    {
        Task SaveAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default);
    }
}
