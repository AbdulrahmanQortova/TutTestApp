using ProtoBuf;
namespace Tut.Common.Models;

[ProtoContract]
public class UserTripPacket
{
    [ProtoMember(1)]
    public UserTripPacketType Type { get; set; } = UserTripPacketType.Unspecified;
    [ProtoMember(2)]
    public string ErrorText { get; set; } = string.Empty;
    [ProtoMember(3)]
    public Trip? Trip { get; set; }
    [ProtoMember(4)]
    public string NotificationText { get; set; } = string.Empty;
    [ProtoMember(5)]
    public List<GLocation> DriverLocations { get; set; } = [];
    [ProtoMember(6)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}


public enum UserTripPacketType
{
    Unspecified = 0,
    Error,
    
    StatusUpdate = 101,              // Server
    DriverLocationUpdate,
    Notification,
    
    GetStatus = 1001,
    RequestTrip,
    CancelTrip,
}

