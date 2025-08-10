using AltMediatR.DDD.Abstractions;
using AltMediatR.WebApiSample.Domain;
using AltMediatR.WebApiSample.Infrastructure;

namespace AltMediatR.WebApiSample.Features.Users;

public sealed record CreateUserCommand(string Name) : ICommand<string>;

public sealed class CreateUserHandler : AltMediatR.Core.Abstractions.IRequestHandler<CreateUserCommand, string>
{
    private readonly AppDbContext _db;

    public CreateUserHandler(AppDbContext db) => _db = db;

    public async Task<string> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User();
        user.Create(request.Name);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}
