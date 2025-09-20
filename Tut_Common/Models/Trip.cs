using ProtoBuf;
using System.ComponentModel.DataAnnotations;

namespace Tut.Common.Models;

[ProtoContract]
public class Trip
{
    [ProtoMember(1)]
    public int Id { get; set; }

    [ProtoMember(2)]
    public required User User { get; set; }
    
    [ProtoMember(3)]
    public Driver? Driver { get; set; }
    
    [ProtoMember(4)]
    public Place? RequestedDriverPlace { get; set; }
    
    [ProtoMember(5)]
    public Place? RequestingPlace { get; set; }
    
    [ProtoMember(6)]
    public DateTime CreatedAt { get; set; } = DateTime.MinValue;
    [ProtoMember(7)]
    public DateTime StartTime { get; set; } = DateTime.MinValue;
    [ProtoMember(8)]
    public DateTime EndTime { get; set; } = DateTime.MinValue;
    [ProtoMember(9)]
    public DateTime CancelTime { get; set; } = DateTime.MinValue;
    
    [ProtoMember(10)]
    public TripState Status { get; set; } = TripState.Unspecified;
    
    [ProtoMember(11)]
    public double EstimatedCost { get; set; }
    [ProtoMember(12)]
    public double ActualCost { get; set; }
    [ProtoMember(13)]
    public int EstimatedArrivalDuration { get; set; }
    [ProtoMember(14)]
    public int ActualArrivalDuration { get; set; }
    [ProtoMember(15)]
    public int EstimatedTripDuration { get; set; }  // Seconds
    [ProtoMember(16)]
    public int ActualTripDuration { get; set; }     // Seconds
    [ProtoMember(17)]
    public int EstimatedDistance { get; set; } = 0;  // Meter
    [ProtoMember(18)]
    public int ActualDistance { get; set; } = 0;  // Meter

    [ProtoMember(19)]
    public List<Stop> Stops { get; set; } = [];

    [ProtoMember(20)]
    public string Route { get; set; } = string.Empty;
}
public enum TripState
{
    Unspecified = 0,
    Requested = 1,
    DriverArrived = 2,
    Started = 3,
    Arrived = 4,
    Ended = 5,
    Canceled = 6
}
