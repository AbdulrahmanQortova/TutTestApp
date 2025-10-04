using ProtoBuf;
namespace Tut.Common.Models;

[ProtoContract]
public class Place
{
    [ProtoMember(1)]
    public int Id { get; set; }
    [ProtoMember(2)]
    public string Name { get; set; } = string.Empty;
    [ProtoMember(3)]
    public string Address { get; set; } = string.Empty;
    [ProtoMember(4, AsReference = true)]
    public GLocation Location { get; set; } = new();
    [ProtoMember(5)]
    public PlaceType PlaceType { get; set; } = PlaceType.Unspecified;
}


public enum PlaceType
{
    Unspecified = 0,
    Saved = 1,
    Recent = 2,
    Trip = 3,
}