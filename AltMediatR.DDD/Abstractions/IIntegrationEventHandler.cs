namespace AltMediatR.DDD.Abstractions
{
    /// <summary>
    /// Optional handler interface for integration events if users want to handle them in-process as notifications too.
    /// Typically integration events are published out-of-process, but this keeps symmetry.
    /// </summary>
    public interface IIntegrationEventHandler<in TEvent>
        where TEvent : IIntegrationEvent
    {
        Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
    }
}
