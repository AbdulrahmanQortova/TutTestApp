using ProtoBuf;
namespace Tut.Common.Models;

[ProtoContract]
public class GLocation
{
    [ProtoMember(1)]
    public double Latitude { get; set; }
    [ProtoMember(2)]
    public double Longitude { get; set; }
    [ProtoMember(3)]
    public double Altitude { get; set; }
    [ProtoMember(4)]
    public double Course { get; set; }
    [ProtoMember(5)]
    public double Speed { get; set; }
    [ProtoMember(6, DataFormat = DataFormat.WellKnown)]
    public DateTime Timestamp { get; set; }
}


