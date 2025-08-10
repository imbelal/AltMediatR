using AltMediatR.Core.Abstractions;

namespace AltMediatR.DDD.Abstractions
{
    /// <summary>
    /// Marker for in-process domain events. These are published and handled within the same process/transaction
    /// via INotificationHandler implementations.
    /// </summary>
    public interface IDomainEvent : INotification
    {
    }
}
