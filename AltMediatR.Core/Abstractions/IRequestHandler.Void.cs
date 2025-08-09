namespace AltMediatR.Core.Abstractions
{
    // Handler for requests that do not produce a response payload
    public interface IRequestHandler<in TRequest> where TRequest : IRequest
    {
        Task HandleAsync(TRequest request, CancellationToken cancellationToken);
    }
}
