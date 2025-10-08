using ProtoBuf;
using System.ComponentModel;

namespace Tut.Common.Models;


[ProtoContract]
public class GIdRequest
{
    [ProtoMember(1)]
    public required int Id { get; init; }
}

[ProtoContract]
public class GIdResponse
{
    [ProtoMember(1)]
    public required int Id { get; init; }
}

[ProtoContract]
public class GStringRequest
{
    [ProtoMember(1)]
    public required string Arg { get; init; } = string.Empty;
}

[ProtoContract]
public class GPartialListRequest
{
    [ProtoMember(1)]
    public int Skip { get; init; } = 0;
    [ProtoMember(2), DefaultValue(50)]
    public int Take { get; init; } = 50;
}

[ProtoContract]
public class GPartialListIdRequest
{
    [ProtoMember(1)]
    public required int Id { get; init; } = 0;
    [ProtoMember(2)]
    public int Skip { get; init; } = 0;
    [ProtoMember(3), DefaultValue(50)]
    public int Take { get; init; } = 50;
}



