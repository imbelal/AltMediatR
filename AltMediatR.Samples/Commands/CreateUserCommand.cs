using AltMediatR.Core.Abstractions;
using AltMediatR.DDD.Abstractions;
using AltMediatR.DDD.Domain;
using AltMediatR.DDD.Infrastructure;

namespace AltMediatR.Samples.Commands
{
    public class CreateUserCommand : ICommand<string>
    {
        public required string Name { get; set; }
    }

    internal sealed class UserAggregate : AggregateRootBase
    {
        public string? Id { get; private set; }
        public string? Name { get; private set; }

        public void Create(string name)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Console.WriteLine($"User '{Name}' created with ID: {Id}");

            RaiseDomainEvent(new AltMediatR.Samples.Events.UserCreatedDomainEvent { UserId = Id!, Name = Name! });
            RaiseIntegrationEvent(new AltMediatR.Samples.Events.UserCreatedIntegrationEvent { UserId = Id!, Name = Name! });
        }
    }

    public class CreateUserHandler : IRequestHandler<CreateUserCommand, string>
    {
        private readonly IEventQueueCollector _collector;

        public CreateUserHandler(IEventQueueCollector collector)
        {
            _collector = collector;
        }

        public Task<string> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var aggregate = new UserAggregate();
            aggregate.Create(request.Name);

            if (_collector is InMemoryEventQueueCollector mem)
                mem.Register(aggregate);

            return Task.FromResult(aggregate.Id!);
        }
    }
}
