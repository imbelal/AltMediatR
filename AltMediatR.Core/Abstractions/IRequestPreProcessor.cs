namespace AltMediatR.Core.Abstractions
{
    public interface IRequestPreProcessor<TRequest>
    {
        Task ProcessAsync(TRequest request, CancellationToken cancellationToken);
    }

}
