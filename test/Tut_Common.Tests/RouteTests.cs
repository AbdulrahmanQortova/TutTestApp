using Tut.Common.Dto.MapDtos;
using Tut.Common.Models;
using Xunit;

namespace Tut.Common.Tests;

public class RouteTests
{
    [Fact]
    public void Route_DefaultConstructor_InitializesEmptyProperties()
    {
        var route = new Route();

        Assert.Empty(route.Points);
        Assert.Equal(string.Empty, route.EncodedPoints);
    }

    [Fact]
    public void Route_WithValidEncodedString_DecodesPolyline()
    {
        // Simple encoded polyline for two points
        var encodedPolyline = "_p~iF~ps|U_ulLnnqC_mqNvxq`@";
        
        var route = new Route(encodedPolyline);

        Assert.Equal(encodedPolyline, route.EncodedPoints);
        Assert.NotEmpty(route.Points);
    }

    [Fact]
    public void Route_WithEmptyEncodedString_HasEmptyPoints()
    {
        var route = new Route(string.Empty);

        Assert.Equal(string.Empty, route.EncodedPoints);
        Assert.Empty(route.Points);
    }

    [Fact]
    public void Route_WithNullEncodedString_HasEmptyPoints()
    {
        var route = new Route((string)null!);

        Assert.Null(route.EncodedPoints);
        Assert.Empty(route.Points);
    }

    [Fact]
    public void Route_FromRouteDto_WithValidPolyline_DecodesCorrectly()
    {
        var routeDto = new RouteDto
        {
            OverviewPolyline = new DirectionPolylineDto
            {
                Points = "_p~iF~ps|U_ulLnnqC_mqNvxq`@"
            }
        };

        var route = new Route(routeDto);

        Assert.NotEmpty(route.Points);
        Assert.NotEqual(string.Empty, route.EncodedPoints);
    }

    [Fact]
    public void Route_FromRouteDto_WithNullPolyline_HasEmptyProperties()
    {
        var routeDto = new RouteDto
        {
            OverviewPolyline = null
        };

        var route = new Route(routeDto);

        Assert.Empty(route.Points);
        Assert.Equal(string.Empty, route.EncodedPoints);
    }

    [Fact]
    public void Route_FromRouteDto_WithNullPoints_HasEmptyProperties()
    {
        var routeDto = new RouteDto
        {
            OverviewPolyline = new DirectionPolylineDto
            {
                Points = null
            }
        };

        var route = new Route(routeDto);

        Assert.Empty(route.Points);
        Assert.Equal(string.Empty, route.EncodedPoints);
    }

    [Fact]
    public void Route_DecodedPoints_HaveLatLngValues()
    {
        var encodedPolyline = "_p~iF~ps|U_ulLnnqC";
        
        var route = new Route(encodedPolyline);

        Assert.NotEmpty(route.Points);
        foreach (var point in route.Points)
        {
            Assert.NotEqual(0, point.Lat);
            Assert.NotEqual(0, point.Lng);
        }
    }

    [Fact]
    public void Route_WithShortPolyline_DecodesWithoutError()
    {
        // Very short encoded polyline
        var encodedPolyline = "??";
        
        var route = new Route(encodedPolyline);

        // Should not throw, should return whatever it can decode
        Assert.NotNull(route.Points);
    }

    [Fact]
    public void Route_WithComplexPolyline_DecodesMultiplePoints()
    {
        // Longer encoded polyline
        var encodedPolyline = "mwjaHzt{tOKDMCCNINM@_@ScAk@mIgEDNDJFHFBNARCHCNM??";
        
        var route = new Route(encodedPolyline);

        Assert.NotEmpty(route.Points);
        // Complex polyline should decode to multiple points
        Assert.True(route.Points.Count > 2);
    }

    [Fact]
    public void Route_DecodedPoints_AreInReasonableRange()
    {
        var encodedPolyline = "_p~iF~ps|U_ulLnnqC_mqNvxq`@";
        
        var route = new Route(encodedPolyline);

        // Latitude should be between -90 and 90
        // Longitude should be between -180 and 180
        foreach (var point in route.Points)
        {
            Assert.InRange(point.Lat, -90, 90);
            Assert.InRange(point.Lng, -180, 180);
        }
    }

    [Fact]
    public void Route_FromStringConstructor_SetsEncodedPoints()
    {
        var encodedPolyline = "_p~iF~ps|U";
        
        var route = new Route(encodedPolyline);

        Assert.Equal(encodedPolyline, route.EncodedPoints);
    }

    [Fact]
    public void Route_FromRouteDtoConstructor_SetsEncodedPoints()
    {
        var encodedPolyline = "_p~iF~ps|U";
        var routeDto = new RouteDto
        {
            OverviewPolyline = new DirectionPolylineDto { Points = encodedPolyline }
        };

        var route = new Route(routeDto);

        Assert.Equal(encodedPolyline, route.EncodedPoints);
    }
}
