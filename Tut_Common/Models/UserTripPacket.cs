using ProtoBuf;
namespace Tut.Common.Models;

[ProtoContract]
public class UserTripPacket
{
    [ProtoMember(1)]
    public required UserTripPacketType Type { get; init; }
    [ProtoMember(2)]
    public string ErrorText { get; init; } = string.Empty;
    [ProtoMember(3)]
    public Trip? Trip { get; init; }
    [ProtoMember(4)]
    public string NotificationText { get; init; } = string.Empty;
    [ProtoMember(5)]
    public List<GLocation>? DriverLocations { get; init; }
    [ProtoMember(6, DataFormat = DataFormat.WellKnown, IsRequired = true)]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;


    public static UserTripPacket Error(string errorText)
    {
        return new UserTripPacket
        {
            Type = UserTripPacketType.Error,
            ErrorText = errorText
        };
    }

    public static UserTripPacket Success()
    {
        return new UserTripPacket
        {
            Type = UserTripPacketType.Success
        };
    }
    
    public static UserTripPacket StatusUpdate(Trip? trip)
    {
        return new UserTripPacket
        {
            Type = UserTripPacketType.StatusUpdate,
            Trip = trip
        };
    }
}


public enum UserTripPacketType
{
    Unspecified = 0,
    Error,
    Success,
    
    StatusUpdate = 101,              // Server
    DriverLocationUpdate,
    Notification,
    
    GetStatus = 1001,
    RequestTrip,
    CancelTrip,
}

