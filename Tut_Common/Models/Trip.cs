using ProtoBuf;
using System.ComponentModel.DataAnnotations;
using Tut.Common.Dto.MapDtos;
using Tut.Common.Utils;

namespace Tut.Common.Models;

[ProtoContract]
public class Trip
{
    [ProtoMember(1)]
    public int Id { get; init; }

    [ProtoMember(2, AsReference = true)]
    public User? User { get; set; }
    
    [ProtoMember(3, AsReference = true)]
    public Driver? Driver { get; set; }
    
    [ProtoMember(4, AsReference = true)]
    public Place? RequestedDriverPlace { get; set; }
    
    [ProtoMember(5, AsReference = true)]
    public Place? RequestingPlace { get; set; }
    
    [ProtoMember(6, DataFormat = DataFormat.WellKnown, IsRequired = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [ProtoMember(7, DataFormat = DataFormat.WellKnown, IsRequired = true)]
    public DateTime DriverArrivalTime { get; set; } = DateTime.MinValue;
    [ProtoMember(8, DataFormat = DataFormat.WellKnown, IsRequired = true)]
    public DateTime StartTime { get; set; } = DateTime.MinValue;
    [ProtoMember(9, DataFormat = DataFormat.WellKnown, IsRequired = true)]
    public DateTime EndTime { get; set; } = DateTime.MinValue;
    [ProtoMember(10, DataFormat = DataFormat.WellKnown, IsRequired = true)]
    public DateTime CancelTime { get; set; } = DateTime.MinValue;
    
    [ProtoMember(11)]
    public TripState Status { get; set; } = TripState.Unspecified;
    [ProtoMember(12)]
    public int TotalWaitTime { get; set; }      // Seconds
    
    [ProtoMember(13)]
    public double EstimatedCost { get; set; }
    [ProtoMember(14)]
    public double ActualCost { get; set; }
    [ProtoMember(15)]
    public int EstimatedArrivalDuration { get; set; } // Seconds
    [ProtoMember(16)]
    public int ActualArrivalDuration { get; set; }  // Seconds
    [ProtoMember(17)]
    public int EstimatedTripDuration { get; set; }  // Seconds
    [ProtoMember(18)]
    public int ActualTripDuration { get; set; }     // Seconds
    [ProtoMember(19)]
    public int EstimatedDistance { get; set; } = 0;  // Meter
    [ProtoMember(20)]
    public int ActualDistance { get; set; } = 0;  // Meter

    [ProtoMember(21)]
    public required List<Place> Stops { get; set; }
    [ProtoMember(22)]
    public int NextStop { get; set; }


    [ProtoMember(23)]
    public string Route { get; private set; } = string.Empty;
    
    public bool IsActive => Status is not (TripState.Unspecified or TripState.Ended or TripState.Canceled);

    public void SetRoute(RouteDto? routeDto)
    {
        Route = routeDto?.OverviewPolyline?.Points ?? string.Empty;
        if (routeDto is not null)
        {
            EstimatedDistance = (int)MapDtoUtils.GetRouteDistance(routeDto);
            EstimatedTripDuration = (int)MapDtoUtils.GetRouteTime(routeDto);
        }
        else
        {
            // calculate the straight line distance, multiply it by 1.5 and set that to EstimationDistance
            EstimatedDistance = (int)(LocationUtils.TotalDistanceInMeters(Stops.Select(s => s.ToLocation())) * 1.5);
            // calculate the Estimated time based on distance, assuming a speed of 50 km/h
            EstimatedTripDuration = (int)(EstimatedDistance / 50_000.0 * 60 * 60);
        }
    }
    
}


[ProtoContract]
public class TripList
{
    [ProtoMember(1, IsRequired = true)]
    public List<Trip> Trips { get; init; } = [];
    public TripList() { }
    public TripList(IEnumerable<Trip> trips)
    {
        Trips = trips.ToList();
    }
}



public enum TripState
{
    Unspecified = 0,
    Requested = 1,
    Acknowledged = 2,
    Accepted = 3,
    DriverArrived = 4,
    Ongoing = 5,
    AtStop = 6,
    Arrived = 20,
    Ended = 21,
    Canceled = 100
}
