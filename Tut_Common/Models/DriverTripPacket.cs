using ProtoBuf;
namespace Tut.Common.Models;

[ProtoContract]
public class DriverTripPacket
{
    [ProtoMember(1)]
    public required DriverTripPacketType Type { get; init; }
    [ProtoMember(2)]
    public string ErrorText { get; init; } = string.Empty;
    [ProtoMember(3)]
    public Trip? Trip { get; init; }
    [ProtoMember(4)]
    public int DistanceTravelledSoFar { get; init; }
    [ProtoMember(5)]
    public int PaymentAmount { get; init; }
    [ProtoMember(6, DataFormat = DataFormat.WellKnown, IsRequired = true)]
    public DateTime? Timestamp { get; init; } = DateTime.UtcNow;

    public static DriverTripPacket Success()
    {
        return new DriverTripPacket
        {
            Type = DriverTripPacketType.Success
        };
    }
    
    public static DriverTripPacket Error(string errorText)
    {
        return new DriverTripPacket
        {
            Type = DriverTripPacketType.Error,
            ErrorText = errorText
        };
    }

    public static DriverTripPacket StatusUpdate(Trip? trip)
    {
        return new DriverTripPacket
        {
            Type = DriverTripPacketType.StatusUpdate,
            Trip = trip
        };
    }
}


public enum DriverTripPacketType
{
    Unspecified = 0,
    Error,
    Success,
    
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
