using ProtoBuf;
namespace Tut.Common.Models;

[ProtoContract]
public class DriverLocation
{
    public int Id { get; set; }
    [ProtoMember(1)]
    public int DriverId { get; set; }
    [ProtoMember(2)]
    public required GLocation Location { get; set; }
}
