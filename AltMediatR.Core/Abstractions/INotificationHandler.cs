namespace AltMediatR.Core.Abstractions
{
    public interface INotificationHandler<in TNotification>
        where TNotification : INotification
    {
        Task HandleAsync(TNotification @event, CancellationToken cancellationToken);
    }

}
