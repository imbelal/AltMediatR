using AltMediatR.Core.Abstractions;

namespace AltMediatR.Samples.Commands
{
    public class DeleteUserCommand : IRequest
    {
        public required string UserId { get; set; }
    }

    public class DeleteUserHandler : IRequestHandler<DeleteUserCommand>
    {
        public Task HandleAsync(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"User deleted with ID: {request.UserId}");
            return Task.CompletedTask;
        }
    }
}
