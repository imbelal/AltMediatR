using AltMediatR.Core.Abstractions;

namespace AltMediatR.DDD.Abstractions
{
    /// <summary>
    /// Marker for cross-boundary integration events. These should be published to a message broker (e.g., Service Bus)
    /// and optionally handled locally.
    /// </summary>
    public interface IIntegrationEvent : INotification
    {
        /// <summary>
        /// A unique id for idempotency when publishing.
        /// </summary>
        Guid Id { get; }
    }
}
