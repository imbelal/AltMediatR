using AltMediatR.Core.Abstractions;

namespace AltMediatR.Samples.Commands
{
    public class CreateUserCommand : IRequest<string>
    {
        public string Name { get; set; }
    }

    public class CreateUserHandler : IRequestHandler<CreateUserCommand, string>
    {
        public Task<string> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var userId = Guid.NewGuid().ToString();
            Console.WriteLine($"User '{request.Name}' created with ID: {userId}");
            return Task.FromResult(userId);
        }
    }
}
