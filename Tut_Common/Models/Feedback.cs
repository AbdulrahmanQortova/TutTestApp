using ProtoBuf;
namespace Tut.Common.Models;

[ProtoContract]
public class Feedback
{
    [ProtoMember(1)]
    public int Id { get; init; }
    [ProtoMember(2)]
    public required Trip Trip { get; init; }
    [ProtoMember(3)]
    public required int Rating { get; init; }
    [ProtoMember(4)]
    public required List<string> Categories { get; init; }
    [ProtoMember(5)]
    public string Comment { get; init; } = string.Empty;
    [ProtoMember(6, DataFormat = DataFormat.WellKnown, IsRequired = true)]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
}
