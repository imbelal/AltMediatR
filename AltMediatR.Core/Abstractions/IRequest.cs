namespace AltMediatR.Core.Abstractions
{
    // Interface for requests expecting a response
    public interface IRequest<out TResponse>;

    // Interface for requests NOT expecting a response (Commands)
    public interface IRequest;
}
