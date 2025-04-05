using AltMediatR.Core.Abstractions;

namespace AltMediatR.Samples.Queries
{
    public class GetUserQuery : IRequest<string>
    {
        public string UserId { get; set; }
    }

    public class GetUserHandler : IRequestHandler<GetUserQuery, string>
    {
        public Task<string> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"User found with ID: {request.UserId}");
        }
    }
}
