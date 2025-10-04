using ProtoBuf;
using System.ComponentModel.DataAnnotations;

namespace Tut.Common.Models;

[ProtoContract]
public class Trip
{
    [ProtoMember(1)]
    public int Id { get; set; }

    [ProtoMember(2, AsReference = true)]
    public required User User { get; set; }
    
    [ProtoMember(3, AsReference = true)]
    public Driver? Driver { get; set; }
    
    [ProtoMember(4, AsReference = true)]
    public Place? RequestedDriverPlace { get; set; }
    
    [ProtoMember(5, AsReference = true)]
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
    
    [ProtoMember(21)]
    public DateTime DriverArrivalTime { get; set; } = DateTime.MinValue;

    public bool IsActive => Status is not (TripState.Unspecified or TripState.Ended or TripState.Canceled);
    
}
public enum TripState
{
    Unspecified = 0,
    Requested = 1,
    Acknowledged = 2,
    Accepted = 3,
    DriverArrived = 4,
    Started = 5,
    AtStop1 = 10,
    AfterStop1 = 11,
    AtStop2 = 12,
    AfterStop2 = 13,
    AtStop3 = 14,
    AfterStop3 = 15,
    AtStop4 = 16,
    AfterStop4 = 17,
    AtStop5 = 18,
    AfterStop5 = 19,
    Arrived = 20,
    Ended = 21,
    Canceled = 100
}
