using Tut.Common.Dto.MapDtos;
using Tut.Common.Utils;
using Xunit;

namespace Tut.Common.Tests;

public class MapDtoUtilsTests
{
    [Fact]
    public void GetRouteDistance_NullLegs_ReturnsZero()
    {
        var routeDto = MockDtos.CreateNullLegsRouteDto();

        double distance = MapDtoUtils.GetRouteDistance(routeDto);

        Assert.Equal(0, distance);
    }

    [Fact]
    public void GetRouteDistance_EmptyLegs_ReturnsZero()
    {
        var routeDto = MockDtos.CreateEmptyRouteDto();

        double distance = MapDtoUtils.GetRouteDistance(routeDto);

        Assert.Equal(0, distance);
    }

    [Fact]
    public void GetRouteDistance_SingleLeg_ReturnsDistance()
    {
        var routeDto = MockDtos.CreateRouteDto(distanceInMeters: 5000);

        double distance = MapDtoUtils.GetRouteDistance(routeDto);

        Assert.Equal(5000, distance);
    }

    [Fact]
    public void GetRouteDistance_MultipleLegs_ReturnsSumOfDistances()
    {
        var routeDto = MockDtos.CreateMultiLegRouteDto(
            (1000, 120),
            (2000, 240),
            (3000, 360)
        );

        double distance = MapDtoUtils.GetRouteDistance(routeDto);

        Assert.Equal(6000, distance);
    }

    [Fact]
    public void GetRouteDistance_LegWithNullDistance_IgnoresLeg()
    {
        var routeDto = new RouteDto
        {
            Legs =
            [
                new LegDto { Distance = new TextValueDto { Value = 1000 } },
                new LegDto { Distance = null },
                new LegDto { Distance = new TextValueDto { Value = 2000 } }
            ]
        };

        double distance = MapDtoUtils.GetRouteDistance(routeDto);

        Assert.Equal(3000, distance);
    }

    [Fact]
    public void GetRouteDistance_LegWithNullDistanceValue_IgnoresLeg()
    {
        var routeDto = new RouteDto
        {
            Legs =
            [
                new LegDto { Distance = new TextValueDto { Value = 1000 } },
                new LegDto { Distance = new TextValueDto { Value = null } },
                new LegDto { Distance = new TextValueDto { Value = 2000 } }
            ]
        };

        double distance = MapDtoUtils.GetRouteDistance(routeDto);

        Assert.Equal(3000, distance);
    }

    [Fact]
    public void GetRouteTime_NullLegs_ReturnsZero()
    {
        var routeDto = MockDtos.CreateNullLegsRouteDto();

        double time = MapDtoUtils.GetRouteTime(routeDto);

        Assert.Equal(0, time);
    }

    [Fact]
    public void GetRouteTime_EmptyLegs_ReturnsZero()
    {
        var routeDto = MockDtos.CreateEmptyRouteDto();

        double time = MapDtoUtils.GetRouteTime(routeDto);

        Assert.Equal(0, time);
    }

    [Fact]
    public void GetRouteTime_SingleLegNoDurationInTraffic_ReturnsDuration()
    {
        var routeDto = MockDtos.CreateRouteDto(durationInSeconds: 600);

        double time = MapDtoUtils.GetRouteTime(routeDto);

        Assert.Equal(600, time);
    }

    [Fact]
    public void GetRouteTime_SingleLegWithDurationInTraffic_ReturnsDurationInTraffic()
    {
        var routeDto = MockDtos.CreateRouteDtoWithTraffic(
            durationInSeconds: 600,
            durationInTrafficSeconds: 900
        );

        double time = MapDtoUtils.GetRouteTime(routeDto);

        Assert.Equal(900, time);
    }

    [Fact]
    public void GetRouteTime_MultipleLegs_ReturnsSumOfDurations()
    {
        var routeDto = MockDtos.CreateMultiLegRouteDto(
            (1000, 120),
            (2000, 240),
            (3000, 360)
        );

        double time = MapDtoUtils.GetRouteTime(routeDto);

        Assert.Equal(720, time);
    }

    [Fact]
    public void GetRouteTime_MixedDurationTypes_PrioritizesDurationInTraffic()
    {
        var routeDto = new RouteDto
        {
            Legs =
            [
                new LegDto
                {
                    Duration = new TextValueDto { Value = 300 },
                    DurationInTraffic = new TextValueDto { Value = 500 }
                },
                new LegDto
                {
                    Duration = new TextValueDto { Value = 200 }
                }
            ]
        };

        double time = MapDtoUtils.GetRouteTime(routeDto);

        // First leg uses DurationInTraffic (500), second uses Duration (200)
        Assert.Equal(700, time);
    }

    [Fact]
    public void GetRouteTime_LegWithNullDuration_IgnoresLeg()
    {
        var routeDto = new RouteDto
        {
            Legs =
            [
                new LegDto { Duration = new TextValueDto { Value = 300 } },
                new LegDto { Duration = null, DurationInTraffic = null },
                new LegDto { Duration = new TextValueDto { Value = 200 } }
            ]
        };

        double time = MapDtoUtils.GetRouteTime(routeDto);

        Assert.Equal(500, time);
    }

    [Fact]
    public void GetRouteTime_LegWithNullDurationValue_IgnoresLeg()
    {
        var routeDto = new RouteDto
        {
            Legs =
            [
                new LegDto { Duration = new TextValueDto { Value = 300 } },
                new LegDto { Duration = new TextValueDto { Value = null } },
                new LegDto { Duration = new TextValueDto { Value = 200 } }
            ]
        };

        double time = MapDtoUtils.GetRouteTime(routeDto);

        Assert.Equal(500, time);
    }

    [Fact]
    public void GetRouteDistance_ZeroDistance_ReturnsZero()
    {
        var routeDto = MockDtos.CreateRouteDto(distanceInMeters: 0);

        double distance = MapDtoUtils.GetRouteDistance(routeDto);

        Assert.Equal(0, distance);
    }

    [Fact]
    public void GetRouteTime_ZeroDuration_ReturnsZero()
    {
        var routeDto = MockDtos.CreateRouteDto(durationInSeconds: 0);

        double time = MapDtoUtils.GetRouteTime(routeDto);

        Assert.Equal(0, time);
    }
}

