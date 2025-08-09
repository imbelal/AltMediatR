namespace AltMediatR.Core.Abstractions
{
    // Marker interface for command requests expecting a response (e.g., id)
    // Extends IRequest<TResponse> to reuse the same handler pipeline
    public interface ICommand<out TResponse> : IRequest<TResponse>;

    // Marker interface for command requests with no response payload
    public interface ICommand : IRequest;
}
