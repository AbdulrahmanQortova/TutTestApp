using Xunit;
using TutBackend;

namespace TutBackend.Tests;

public class ProgramTests
{
    [Fact]
    public void Program_Default_ConnectionString_IsEmpty()
    {
        Assert.Equal(string.Empty, Program.ConnectionString);
    }

    [Fact]
    public void Program_Type_Exists()
    {
        var type = typeof(Program);
        Assert.NotNull(type);
        Assert.Equal("TutBackend.Program", type.FullName);
    }
}

