using AltMediatR.DDD.Abstractions;

namespace AltMediatR.Samples.Events
{
    public sealed class UserCreatedDomainEvent : IDomainEvent
    {
        public required string UserId { get; init; }
        public required string Name { get; init; }
    }

    public sealed class UserCreatedDomainEventHandler : AltMediatR.Core.Abstractions.INotificationHandler<UserCreatedDomainEvent>
    {
        public async Task HandleAsync(UserCreatedDomainEvent notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[DomainEvent] User created: {notification.UserId} - {notification.Name}");
            await Task.CompletedTask;
        }
    }
}
