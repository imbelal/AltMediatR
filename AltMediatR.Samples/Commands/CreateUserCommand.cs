using AltMediatR.Core.Abstractions;

namespace AltMediatR.Samples.Commands
{
    public class CreateUserCommand : ICommand<string>
    {
        public string Name { get; set; }
    }

    public class CreateUserHandler : IRequestHandler<CreateUserCommand, string>
    {
        private readonly IDomainEventQueue _domainEvents;
        private readonly IIntegrationEventQueue _integrationEvents;

        public CreateUserHandler(IDomainEventQueue domainEvents, IIntegrationEventQueue integrationEvents)
        {
            _domainEvents = domainEvents;
            _integrationEvents = integrationEvents;
        }

        public Task<string> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var userId = Guid.NewGuid().ToString();
            Console.WriteLine($"User '{request.Name}' created with ID: {userId}");

            // Queue domain event (in-process)
            _domainEvents.Enqueue(new AltMediatR.Samples.Events.UserCreatedDomainEvent
            {
                UserId = userId,
                Name = request.Name
            });

            // Queue integration event (out-of-process)
            _integrationEvents.Enqueue(new AltMediatR.Samples.Events.UserCreatedIntegrationEvent
            {
                UserId = userId,
                Name = request.Name
            });

            return Task.FromResult(userId);
        }
    }
}
