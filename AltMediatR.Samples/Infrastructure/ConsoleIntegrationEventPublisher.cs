using AltMediatR.Core.Abstractions;
using System.Text.Json;

namespace AltMediatR.Samples.Infrastructure
{
    /// <summary>
    /// Sample IIntegrationEventPublisher that writes to console. Replace with Service Bus implementation in real apps.
    /// </summary>
    public sealed class ConsoleIntegrationEventPublisher : IIntegrationEventPublisher
    {
        public Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[IntegrationEvent] {@event.GetType().Name}: {JsonSerializer.Serialize(@event)}");
            return Task.CompletedTask;
        }
    }
}
