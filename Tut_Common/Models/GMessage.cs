using ProtoBuf;

namespace Tut.Common.Models;

[ProtoContract]
public class GMessage
{
    [ProtoMember(1)]
    public int Id { get; init; }
    [ProtoMember(2)]
    public required int SenderId { get; init; }
    [ProtoMember(3)]
    public required int RecipientId { get; init; }
    [ProtoMember(4)]
    public required string Content { get; set; }
    [ProtoMember(5, DataFormat = DataFormat.WellKnown, IsRequired = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

