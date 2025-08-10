using AltMediatR.Core.Abstractions;
using Microsoft.Extensions.Logging;
using AltMediatR.DDD.Abstractions;

namespace AltMediatR.WebApiSample.Domain;

public sealed class UserCreatedDomainEventHandler : IDomainEventHandler<UserCreatedDomainEvent>
{
    private readonly ILogger<UserCreatedDomainEventHandler> _logger;
    public UserCreatedDomainEventHandler(ILogger<UserCreatedDomainEventHandler> logger) => _logger = logger;

    public Task HandleAsync(UserCreatedDomainEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[DOMAIN] User created: {UserId} - {Name}", @event.UserId, @event.Name);
        return Task.CompletedTask;
    }
}

public sealed class OrderPlacedDomainEventHandler : IDomainEventHandler<OrderPlacedDomainEvent>
{
    private readonly ILogger<OrderPlacedDomainEventHandler> _logger;
    public OrderPlacedDomainEventHandler(ILogger<OrderPlacedDomainEventHandler> logger) => _logger = logger;

    public Task HandleAsync(OrderPlacedDomainEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[DOMAIN] Order placed: {OrderId} for User {UserId} Total {Total}", @event.OrderId, @event.UserId, @event.Total);
        return Task.CompletedTask;
    }
}

// Integration event handlers for inbound processing
public sealed class UserCreatedIntegrationEventHandler : IIntegrationEventHandler<UserCreatedIntegrationEvent>
{
    private readonly ILogger<UserCreatedIntegrationEventHandler> _logger;
    public UserCreatedIntegrationEventHandler(ILogger<UserCreatedIntegrationEventHandler> logger) => _logger = logger;

    public Task HandleAsync(UserCreatedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[INTEGRATION] User created received: {UserId} - {Name}", @event.UserId, @event.Name);
        return Task.CompletedTask;
    }
}

public sealed class OrderPlacedIntegrationEventHandler : IIntegrationEventHandler<OrderPlacedIntegrationEvent>
{
    private readonly ILogger<OrderPlacedIntegrationEventHandler> _logger;
    public OrderPlacedIntegrationEventHandler(ILogger<OrderPlacedIntegrationEventHandler> logger) => _logger = logger;

    public Task HandleAsync(OrderPlacedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[INTEGRATION] Order placed received: {OrderId} for User {UserId} Total {Total}", @event.OrderId, @event.UserId, @event.Total);
        return Task.CompletedTask;
    }
}
