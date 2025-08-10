using AltMediatR.DDD.Abstractions;
using AltMediatR.WebApiSample.Domain;
using AltMediatR.WebApiSample.Infrastructure;

namespace AltMediatR.WebApiSample.Features.Orders;

public sealed record PlaceOrderCommand(string UserId, decimal Total) : ICommand<string>;

public sealed class PlaceOrderHandler : AltMediatR.Core.Abstractions.IRequestHandler<PlaceOrderCommand, string>
{
    private readonly AppDbContext _db;

    public PlaceOrderHandler(AppDbContext db) => _db = db;

    public async Task<string> HandleAsync(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order();
        order.Create(request.UserId, request.Total);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);
        return order.Id;
    }
}
