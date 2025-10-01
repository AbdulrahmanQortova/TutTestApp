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

[ProtoContract]
public class GPartialListRequest
{
    [ProtoMember(1)]
    public int Skip { get; set; } = 0;
    [ProtoMember(2)]
    public int Take { get; set; } = 50;
}

[ProtoContract]
public class GPartialListIdRequest
{
    [ProtoMember(1)]
    public int Id { get; set; } = 0;
    [ProtoMember(2)]
    public int Skip { get; set; } = 0;
    [ProtoMember(3)]
    public int Take { get; set; } = 50;
}



