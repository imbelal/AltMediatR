using AltMediatR.Core.Abstractions;

namespace AltMediatR.Samples.Events
{
    public sealed class UserCreatedDomainEvent : IDomainEvent
    {
        public required string UserId { get; init; }
        public required string Name { get; init; }
    }

    public sealed class UserCreatedDomainEventHandler : INotificationHandler<UserCreatedDomainEvent>
    {
        public async Task HandleAsync(UserCreatedDomainEvent notification, CancellationToken cancellationToken)
        {
            // Simulate same-transaction side effect (e.g., write audit log)
            Console.WriteLine($"[DomainEvent] User created: {notification.UserId} - {notification.Name}");
            await Task.CompletedTask;
        }
    }
}
