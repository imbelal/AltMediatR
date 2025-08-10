using AltMediatR.Core.Abstractions;
using AltMediatR.DDD.Abstractions;

namespace AltMediatR.Samples.Queries
{
    public class GetUserQuery : IQuery<string>, ICacheable
    {
        public required string UserId { get; set; }

        public string CacheKey => $"GetUser:{UserId}";
        public TimeSpan? AbsoluteExpirationRelativeToNow => TimeSpan.FromMinutes(2);
    }

    public class GetUserHandler : IRequestHandler<GetUserQuery, string>
    {
        public Task<string> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"User found with ID: {request.UserId}");
        }
    }
}
