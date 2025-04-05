namespace AltMediatR.Core.Abstractions
{
    public interface IRequestPostProcessor<TRequest, TResponse>
    {
        Task ProcessAsync(TRequest request, TResponse response, CancellationToken cancellationToken);
    }
}
