using Tut.Common.Utils;
using Xunit;

namespace Tut.Common.Tests;

public class JsonExtensionsTests
{
    [Fact]
    public void ToJson_SimpleObject_ReturnsJsonString()
    {
        var obj = new { Name = "John", Age = 30 };

        string json = obj.ToJson();

        Assert.Contains("\"Name\"", json);
        Assert.Contains("\"John\"", json);
        Assert.Contains("\"Age\"", json);
        Assert.Contains("30", json);
    }

    [Fact]
    public void ToJson_WithIndent_ReturnsFormattedJson()
    {
        var obj = new { Name = "John", Age = 30 };

        string json = obj.ToJson(indent: true);

        Assert.Contains("\n", json); // Should contain newlines for formatting
        Assert.Contains("  ", json); // Should contain indentation
    }

    [Fact]
    public void ToJson_WithoutIndent_ReturnsCompactJson()
    {
        var obj = new { Name = "John" };

        string json = obj.ToJson(indent: false);

        Assert.DoesNotContain("\n", json); // Should not contain newlines
        Assert.DoesNotContain("  ", json); // Should not contain extra spaces
    }

    [Fact]
    public void ToJson_ComplexObject_SerializesCorrectly()
    {
        var obj = new
        {
            User = new { Id = 1, Name = "John" },
            Items = new[] { "item1", "item2" }
        };

        string json = obj.ToJson();

        Assert.Contains("User", json);
        Assert.Contains("Items", json);
        Assert.Contains("item1", json);
        Assert.Contains("item2", json);
    }

    [Fact]
    public void ToJson_NullValues_HandlesCorrectly()
    {
        var obj = new { Name = "John", Email = (string?)null };

        string json = obj.ToJson();

        Assert.Contains("Name", json);
        Assert.Contains("John", json);
    }

    [Fact]
    public void ToJson_EmptyObject_ReturnsEmptyJsonObject()
    {
        var obj = new { };

        string json = obj.ToJson();

        Assert.Contains("{", json);
        Assert.Contains("}", json);
    }

    [Fact]
    public void ToJson_Array_SerializesCorrectly()
    {
        var array = new[] { 1, 2, 3, 4, 5 };

        string json = array.ToJson();

        Assert.Contains("[", json);
        Assert.Contains("]", json);
        Assert.Contains("1", json);
        Assert.Contains("5", json);
    }

    [Fact]
    public void ToJson_String_SerializesAsJsonString()
    {
        var str = "Hello World";

        string json = str.ToJson();

        Assert.Contains("Hello World", json);
    }

    [Fact]
    public void ToJson_Number_SerializesAsJsonNumber()
    {
        var number = 42;

        string json = number.ToJson();

        Assert.Equal("42", json);
    }

    [Fact]
    public void ToJson_Boolean_SerializesAsJsonBoolean()
    {
        var boolTrue = true;
        var boolFalse = false;

        string jsonTrue = boolTrue.ToJson();
        string jsonFalse = boolFalse.ToJson();

        Assert.Equal("true", jsonTrue);
        Assert.Equal("false", jsonFalse);
    }
}

