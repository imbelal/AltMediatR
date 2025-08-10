using AltMediatR.Core.Abstractions;

namespace AltMediatR.Samples.Events
{
    public sealed class UserCreatedIntegrationEvent : IIntegrationEvent
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public required string UserId { get; init; }
        public required string Name { get; init; }
    }
}
