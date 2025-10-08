using ProtoBuf;
namespace Tut.Common.Models;

[ProtoContract]
public class Place
{
    [ProtoMember(1)]
    public int Id { get; init; }
    [ProtoMember(2)]
    public string Name { get; set; } = string.Empty;
    [ProtoMember(3)]
    public string Address { get; set; } = string.Empty;
    [ProtoMember(5)]
    public required PlaceType PlaceType { get; init; }
    [ProtoMember(6)]
    public required double Latitude { get; init; }
    [ProtoMember(7)]
    public required double Longitude { get; init; }
    [ProtoMember(8, AsReference = true)]
    public User? User { get; init; }
    [ProtoMember(10, AsReference = true)]
    public int Order { get; init; }


    public GLocation ToLocation()
    {
        return new GLocation
        {
            Latitude = Latitude,
            Longitude = Longitude,
        };
    }
}


public enum PlaceType
{
    Unspecified = 0,
    Saved = 1,
    Recent = 2,
    Stop = 3,
}