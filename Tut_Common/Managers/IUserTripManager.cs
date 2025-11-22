using Tut.Common.Models;
namespace Tut.Common.Managers;

public interface IUserTripManager
{
    ConnectionState CurrentState { get; }
    Trip? CurrentTrip { get; }
    event EventHandler<StatusUpdateEventArgs>? StatusChanged;
    event EventHandler<ErrorReceivedEventArgs>? ErrorReceived;
    event EventHandler<NotificationReceivedEventArgs>? NotificationReceived;
    event EventHandler<DriverLocationsReceivedEventArgs>? DriverLocationsReceived;
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    event EventHandler<InquireResultEventArgs>? InquireResultReceived;
    Task Connect(CancellationToken cancellationToken);
    Task SendInquireTripAsync(Trip trip, CancellationToken cancellationToken = default);
    Task SendRequestTripAsync(Trip trip, CancellationToken cancellationToken = default);
    Task SendCancelTripAsync(CancellationToken cancellationToken = default);
    Task SendAsync(UserTripPacket packet, CancellationToken cancellationToken = default);
    Task Disconnect();
}

public class InquireResultEventArgs : EventArgs
{
    public Trip? Trip { get; set; }
}
public class StatusUpdateEventArgs : EventArgs
{
    public Trip? Trip { get; set; }
}
public class ErrorReceivedEventArgs : EventArgs
{
    public string ErrorText { get; set; } = string.Empty;
}
public class NotificationReceivedEventArgs : EventArgs
{
    public string NotificationText { get; set; } = string.Empty;
}
public class DriverLocationsReceivedEventArgs : EventArgs
{
    public List<GLocation> Locations { get; set; } = [];
}

public class ConnectionStateChangedEventArgs : EventArgs
{
    public ConnectionState OldState { get; set; }
    public ConnectionState NewState { get; set; }
}

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting
}
