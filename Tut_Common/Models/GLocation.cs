using ProtoBuf;
namespace Tut.Common.Models;

[ProtoContract]
public class GLocation
{
    [ProtoMember(1)]
    public int Id { get; init; }
    [ProtoMember(2)]
    public required double Latitude { get; init; }
    [ProtoMember(3)]
    public required double Longitude { get; init; }
    [ProtoMember(4)]
    public double Altitude { get; init; }
    [ProtoMember(5)]
    public double Course { get; init; }
    [ProtoMember(6)]
    public double Speed { get; init; }
    [ProtoMember(7, DataFormat = DataFormat.WellKnown, IsRequired = true)]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}


