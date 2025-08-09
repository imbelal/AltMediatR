namespace AltMediatR.Core.Abstractions
{
    // Marker interface for query requests expecting a response
    // Extends IRequest<TResponse> so existing handlers and pipeline continue to work
    public interface IQuery<out TResponse> : IRequest<TResponse>;
}
