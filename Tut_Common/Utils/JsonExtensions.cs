using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tut.Common.Utils;

public static class JsonExtensions
{
    private static readonly JsonSerializerOptions Pretty = new () { WriteIndented = true , ReferenceHandler = ReferenceHandler.IgnoreCycles};
    public static string ToJson(this object o, bool indent = true)
    {
        return !indent ? JsonSerializer.Serialize(o) : JsonSerializer.Serialize(o, Pretty);
    }
}