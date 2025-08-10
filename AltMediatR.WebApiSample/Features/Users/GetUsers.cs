using AltMediatR.DDD.Abstractions;
using AltMediatR.WebApiSample.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AltMediatR.WebApiSample.Features.Users;

public sealed record GetUsersQuery() : IQuery<IReadOnlyList<UserDto>>, ICacheable
{
    public string CacheKey => "users:all";
    public TimeSpan? AbsoluteExpirationRelativeToNow => TimeSpan.FromSeconds(30);
}

public sealed record UserDto(string Id, string Name);

public sealed class GetUsersHandler : AltMediatR.Core.Abstractions.IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    private readonly AppDbContext _db;

    public GetUsersHandler(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<UserDto>> HandleAsync(GetUsersQuery request, CancellationToken cancellationToken)
    {
        return await _db.Users
            .AsNoTracking()
            .Select(u => new UserDto(u.Id, u.Name))
            .ToListAsync(cancellationToken);
    }
}
