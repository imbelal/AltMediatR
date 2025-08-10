using AltMediatR.DDD.Domain;
using AltMediatR.DDD.Abstractions;

namespace AltMediatR.WebApiSample.Domain;

public sealed class User : AggregateRootBase
{
    public string Id { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    public void Create(string name)
    {
        Id = Guid.NewGuid().ToString();
        Name = name;
        RaiseDomainEvent(new UserCreatedDomainEvent { UserId = Id, Name = Name });
        RaiseIntegrationEvent(new UserCreatedIntegrationEvent { UserId = Id, Name = Name });
    }
}

public sealed class UserCreatedDomainEvent : IDomainEvent
{
    public required string UserId { get; init; }
    public required string Name { get; init; }
}

public sealed class UserCreatedIntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string UserId { get; init; }
    public required string Name { get; init; }
}
