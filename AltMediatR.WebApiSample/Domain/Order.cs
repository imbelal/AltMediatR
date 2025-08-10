using AltMediatR.DDD.Domain;
using AltMediatR.DDD.Abstractions;

namespace AltMediatR.WebApiSample.Domain;

public sealed class Order : AggregateRootBase
{
    public string Id { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public decimal Total { get; private set; }

    public void Create(string userId, decimal total)
    {
        Id = Guid.NewGuid().ToString();
        UserId = userId;
        Total = total;
        RaiseDomainEvent(new OrderPlacedDomainEvent { OrderId = Id, UserId = userId, Total = total });
        RaiseIntegrationEvent(new OrderPlacedIntegrationEvent { OrderId = Id, UserId = userId, Total = total });
    }
}

public sealed class OrderPlacedDomainEvent : IDomainEvent
{
    public required string OrderId { get; init; }
    public required string UserId { get; init; }
    public required decimal Total { get; init; }
}

public sealed class OrderPlacedIntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string OrderId { get; init; }
    public required string UserId { get; init; }
    public required decimal Total { get; init; }
}
