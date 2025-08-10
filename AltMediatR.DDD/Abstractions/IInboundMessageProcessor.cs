namespace AltMediatR.DDD.Abstractions
{
    /// <summary>
    /// Orchestrates an inbound subscription/pump and dispatches messages to in-process integration event handlers.
    /// Implementations can wrap a transport-specific listener.
    /// </summary>
    public interface IInboundMessageProcessor
    {
        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
    }
}
