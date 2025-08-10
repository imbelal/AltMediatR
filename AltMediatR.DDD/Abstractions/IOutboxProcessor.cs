namespace AltMediatR.DDD.Abstractions
{
    /// <summary>
    /// Processes pending integration events from an outbox store and publishes them to the configured publisher.
    /// </summary>
    public interface IOutboxProcessor
    {
        /// <summary>
        /// Runs one processing cycle: fetch pending events, publish, and mark as published.
        /// </summary>
        Task ProcessOnceAsync(CancellationToken cancellationToken = default);
    }
}
