using ProtoBuf;
namespace Tut.Common.Models;

[ProtoContract]
public class DriverLocation
{
    [ProtoMember(1)]
    public int Id { get; init; }
    [ProtoMember(2)]
    public required int DriverId { get; init; }
    [ProtoMember(3)]
    public required string DriverName { get; init; } = string.Empty;
    [ProtoMember(4)]
    public DriverState DriverState { get; init; } = DriverState.Unspecified;
    [ProtoMember(5, AsReference = true)]
    public Trip? Trip { get; init; }
    [ProtoMember(6)]
    public required double Latitude { get; init; }
    [ProtoMember(7)]
    public required double Longitude { get; init; }
    [ProtoMember(8)]
    public double Altitude { get; init; }
    [ProtoMember(9)]
    public double Course { get; init; }
    [ProtoMember(10)]
    public double Speed { get; init; }
    [ProtoMember(11, DataFormat = DataFormat.WellKnown, IsRequired = true)]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public GLocation ToLocation()
    {
        return new GLocation
        {
            Latitude = Latitude,
            Longitude = Longitude,
            Altitude = Altitude,
            Course = Course,
            Speed = Speed,
            Timestamp = Timestamp
        };
    }
}

[ProtoContract]
public class DriverLocationList
{
    [ProtoMember(1)]
    public List<DriverLocation>? Locations { get; init; }

    public DriverLocationList() {}
    public DriverLocationList(IEnumerable<DriverLocation> locations)
    {
        Locations = locations.ToList();
    }
}
