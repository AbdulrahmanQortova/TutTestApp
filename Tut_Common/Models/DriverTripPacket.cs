using ProtoBuf;
namespace Tut.Common.Models;

[ProtoContract]
public class DriverTripPacket
{
    [ProtoMember(1)]
    public DriverTripPacketType Type { get; set; } = DriverTripPacketType.Unspecified;
    [ProtoMember(2)]
    public string ErrorText { get; set; } = string.Empty;
    [ProtoMember(3)]
    public Trip? Trip { get; set; }
    [ProtoMember(4)]
    public int DistanceTravelledSoFar { get; set; }
    [ProtoMember(5)]
    public DateTime? Timestamp { get; set; } = DateTime.UtcNow;
}


public enum DriverTripPacketType
{
    Unspecified = 0,
    Error,
    
    OfferTrip = 101,                  // Server
    StatusUpdate,                     // Server
    
    
    PunchIn = 1001,
    PunchOut,
    TripReceived,
    AcceptTrip,
    ArrivedAtPickup,
    StartTrip,
    ArrivedAtStop,
    ContinueTrip,
    ArrivedAtDestination,
    CashPaymentMade,
    GetStatus

}
