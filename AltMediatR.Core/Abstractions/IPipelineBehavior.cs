using AltMediatR.Core.Delegates;

namespace AltMediatR.Core.Abstractions
{
    public interface IPipelineBehavior<TRequest, TResponse>
    {
        Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next);
    }
}
