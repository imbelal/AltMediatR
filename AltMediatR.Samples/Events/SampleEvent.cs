using AltMediatR.Core.Abstractions;

namespace AltMediatR.Samples.Events
{
    public class SampleEvent : INotification
    {
        public required string Message { get; set; }
    }

    public class SampleEventHandler : INotificationHandler<SampleEvent>
    {
        public async Task HandleAsync(SampleEvent @event, CancellationToken cancellationToken)
        {
            // Log the received event's message
            Console.WriteLine($"Notification received: {@event.Message}");

            // Simulate some async operation (e.g., sending a message or updating a system)
            await Task.Delay(1000);

            // Log completion
            Console.WriteLine("Notification processed successfully.");
        }
    }

    public class SecondSampleEventHandler : INotificationHandler<SampleEvent>
    {

        public async Task HandleAsync(SampleEvent @event, CancellationToken cancellationToken)
        {
            // Log the received event's message
            Console.WriteLine($"Notification received: {@event.Message}");

            // Simulate some async operation (e.g., sending a message or updating a system)
            await Task.Delay(500);

            // Log completion
            Console.WriteLine("Notification processed successfully.");
        }
    }
}
