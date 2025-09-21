using ProtoBuf;

namespace Tut.Common.Models;

[ProtoContract]
public class GMessage
{
    public int Id { get; set; }
    [ProtoMember(1)]
    public int SenderId { get; set; }
    [ProtoMember(2)]
    public int RecipientId { get; set; }
    [ProtoMember(3)]
    public string Content { get; set; } = string.Empty;
    [ProtoMember(4, DataFormat = DataFormat.WellKnown)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

