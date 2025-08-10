using AltMediatR.DDD.Abstractions;
using AltMediatR.WebApiSample.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AltMediatR.WebApiSample.Features.Orders;

public sealed record GetOrdersQuery() : IQuery<IReadOnlyList<OrderDto>>, ICacheable
{
    public string CacheKey => "orders:all";
    public TimeSpan? AbsoluteExpirationRelativeToNow => TimeSpan.FromSeconds(30);
}

public sealed record OrderDto(string Id, string UserId, decimal Total);

public sealed class GetOrdersHandler : AltMediatR.Core.Abstractions.IRequestHandler<GetOrdersQuery, IReadOnlyList<OrderDto>>
{
    private readonly AppDbContext _db;

    public GetOrdersHandler(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<OrderDto>> HandleAsync(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        return await _db.Orders
            .AsNoTracking()
            .Select(o => new OrderDto(o.Id, o.UserId, o.Total))
            .ToListAsync(cancellationToken);
    }
}
