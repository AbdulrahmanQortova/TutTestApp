using Xunit;
using TutBackend;
using TutBackend.Services;

namespace TutBackend.Tests;

public class ProgramTests
{
    [Fact]
    public void Program_Default_ConnectionString_IsEmpty()
    {
        DriverCache.Clear();
        Assert.Equal(string.Empty, Program.ConnectionString);
    }

    [Fact]
    public void Program_Type_Exists()
    {
        DriverCache.Clear();
        var type = typeof(Program);
        Assert.NotNull(type);
        Assert.Equal("TutBackend.Program", type.FullName);
    }
}
