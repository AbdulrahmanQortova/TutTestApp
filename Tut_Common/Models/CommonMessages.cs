using ProtoBuf;
namespace Tut.Common.Models;


[ProtoContract]
public class GIdRequest
{
    [ProtoMember(1)]
    public int Id { get; set; }
}

[ProtoContract]
public class GIdResponse
{
    [ProtoMember(1)]
    public int Id { get; set; }
}

[ProtoContract]
public class GStringRequest
{
    [ProtoMember(1)]
    public string Arg { get; set; } = string.Empty;
}


