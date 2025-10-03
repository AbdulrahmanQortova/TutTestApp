using ProtoBuf;
namespace Tut.Common.Models;

[ProtoContract]
public class Feedback
{
    [ProtoMember(1)]
    public int TripId { get; set; }
    [ProtoMember(2)]
    public int Rating { get; set; }
    [ProtoMember(3)]
    public List<string> Categories { get; set; } = [];
    [ProtoMember(4)]
    public string Comment { get; set; } = string.Empty;
    
}
