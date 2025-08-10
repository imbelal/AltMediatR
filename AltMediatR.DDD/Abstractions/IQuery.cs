namespace AltMediatR.DDD.Abstractions
{
    public interface IQuery<out TResponse> : AltMediatR.Core.Abstractions.IRequest<TResponse>;
}
