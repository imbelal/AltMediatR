namespace AltMediatR.WebApiSample.Contracts;

public sealed record CreateUserRequest(string Name);
public sealed record CreateOrderRequest(string UserId, decimal Total);
