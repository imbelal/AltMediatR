using AltMediatR.DDD.Abstractions;

namespace AltMediatR.DDD.Abstractions
{
    public interface ICommand<out TResponse> : AltMediatR.Core.Abstractions.IRequest<TResponse>;
}
