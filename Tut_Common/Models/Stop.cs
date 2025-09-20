using ProtoBuf;

namespace Tut.Common.Models;

[ProtoContract]
public class Stop
{
    public int Id { get; set; }
    public int TripId { get; set; }
    [ProtoMember(1)]
    public Place? Place { get; set; }
}

