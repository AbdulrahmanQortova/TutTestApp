using ProtoBuf;
namespace Tut.Common.Models;

[ProtoContract]
public class SavedPlace
{
    public int Id { get; set; }
    [ProtoMember(1, AsReference = true)]
    public required Place Place { get; set; }
}
