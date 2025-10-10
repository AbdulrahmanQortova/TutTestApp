using Tut.Common.Models;
using Xunit;

namespace Tut.Common.Tests;

public class CommonMessagesTests
{
    [Fact]
    public void GIdRequest_CanBeCreated_WithId()
    {
        var request = new GIdRequest { Id = 42 };

        Assert.Equal(42, request.Id);
    }

    [Fact]
    public void GIdResponse_CanBeCreated_WithId()
    {
        var response = new GIdResponse { Id = 100 };

        Assert.Equal(100, response.Id);
    }

    [Fact]
    public void GStringRequest_CanBeCreated_WithArg()
    {
        var request = new GStringRequest { Arg = "test-value" };

        Assert.Equal("test-value", request.Arg);
    }

    [Fact]
    public void GStringRequest_DefaultArg_IsEmptyString()
    {
        var request = new GStringRequest { Arg = string.Empty };

        Assert.Equal(string.Empty, request.Arg);
    }

    [Fact]
    public void GPartialListRequest_DefaultValues_AreSetCorrectly()
    {
        var request = new GPartialListRequest();

        Assert.Equal(0, request.Skip);
        Assert.Equal(50, request.Take);
    }

    [Fact]
    public void GPartialListRequest_WithCustomValues_SetsCorrectly()
    {
        var request = new GPartialListRequest
        {
            Skip = 20,
            Take = 100
        };

        Assert.Equal(20, request.Skip);
        Assert.Equal(100, request.Take);
    }

    [Fact]
    public void GPartialListIdRequest_RequiredId_CanBeSet()
    {
        var request = new GPartialListIdRequest { Id = 5 };

        Assert.Equal(5, request.Id);
    }

    [Fact]
    public void GPartialListIdRequest_DefaultValues_AreSetCorrectly()
    {
        var request = new GPartialListIdRequest { Id = 1 };

        Assert.Equal(1, request.Id);
        Assert.Equal(0, request.Skip);
        Assert.Equal(50, request.Take);
    }

    [Fact]
    public void GPartialListIdRequest_WithAllValues_SetsCorrectly()
    {
        var request = new GPartialListIdRequest
        {
            Id = 10,
            Skip = 30,
            Take = 75
        };

        Assert.Equal(10, request.Id);
        Assert.Equal(30, request.Skip);
        Assert.Equal(75, request.Take);
    }

    [Fact]
    public void GPartialListRequest_ForPagination_FirstPage()
    {
        var request = new GPartialListRequest
        {
            Skip = 0,
            Take = 10
        };

        Assert.Equal(0, request.Skip);
        Assert.Equal(10, request.Take);
    }

    [Fact]
    public void GPartialListRequest_ForPagination_SecondPage()
    {
        var request = new GPartialListRequest
        {
            Skip = 10,
            Take = 10
        };

        Assert.Equal(10, request.Skip);
        Assert.Equal(10, request.Take);
    }

    [Fact]
    public void GPartialListIdRequest_ForUserSpecificPagination()
    {
        var request = new GPartialListIdRequest
        {
            Id = 123,
            Skip = 0,
            Take = 25
        };

        Assert.Equal(123, request.Id);
        Assert.Equal(0, request.Skip);
        Assert.Equal(25, request.Take);
    }
}

