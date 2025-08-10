using System.Threading;
using System.Threading.Tasks;

namespace AltMediatR.Core.Abstractions
{
    /// <summary>
    /// Abstraction to publish integration events to a message broker.
    /// Implement this in the application with Azure Service Bus, RabbitMQ, etc.
    /// </summary>
    public interface IIntegrationEventPublisher
    {
        Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default);
    }
}
