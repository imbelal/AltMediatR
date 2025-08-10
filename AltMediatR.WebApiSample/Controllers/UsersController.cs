using AltMediatR.Core.Abstractions;
using AltMediatR.WebApiSample.Features.Users;
using Microsoft.AspNetCore.Mvc;
using AltMediatR.WebApiSample.Contracts;

namespace AltMediatR.WebApiSample.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<string>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var id = await _mediator.SendAsync(new CreateUserCommand(request.Name), ct);
        return Ok(id);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken ct)
    {
        var users = await _mediator.SendAsync(new GetUsersQuery(), ct);
        return Ok(users);
    }
}
