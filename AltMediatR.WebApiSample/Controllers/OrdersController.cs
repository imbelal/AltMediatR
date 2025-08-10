using AltMediatR.Core.Abstractions;
using AltMediatR.WebApiSample.Features.Orders;
using Microsoft.AspNetCore.Mvc;
using AltMediatR.WebApiSample.Contracts;

namespace AltMediatR.WebApiSample.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<string>> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var id = await _mediator.SendAsync(new PlaceOrderCommand(request.UserId, request.Total), ct);
        return Ok(id);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAll(CancellationToken ct)
    {
        var orders = await _mediator.SendAsync(new GetOrdersQuery(), ct);
        return Ok(orders);
    }
}
