using AltMediatR.Core.Abstractions;

namespace AltMediatR.Samples.Events
{
    public class AnotherEvent : INotification
    {
        public string Message { get; set; }
    }

    public class AnotherEventHandler : INotificationHandler<AnotherEvent>
    {

        public async Task HandleAsync(AnotherEvent @event, CancellationToken cancellationToken)
        {
            // Log the received event's message
            Console.WriteLine($"Notification received: {@event.Message}");

            // Simulate some async operation (e.g., sending a message or updating a system)
            await Task.Delay(1500);

            // Log completion
            Console.WriteLine("Notification processed successfully.");
        }
    }
}
