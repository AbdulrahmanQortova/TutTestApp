using Tut.Common.Dto.MapDtos;

namespace Tut.Common.Tests;

/// <summary>
/// Mock DTOs for testing without using a mocking framework
/// </summary>
public static class MockDtos
{
    public static RouteDto CreateRouteDto(
        double distanceInMeters = 5000,
        double durationInSeconds = 600,
        string? polylinePoints = null)
    {
        return new RouteDto
        {
            OverviewPolyline = polylinePoints != null ? new DirectionPolylineDto { Points = polylinePoints } : null,
            Legs =
            [
                new LegDto
                {
                    Distance = new TextValueDto { Value = distanceInMeters, Text = $"{distanceInMeters}m" },
                    Duration = new TextValueDto { Value = durationInSeconds, Text = $"{durationInSeconds}s" }
                }
            ]
        };
    }

    public static RouteDto CreateRouteDtoWithTraffic(
        double distanceInMeters = 5000,
        double durationInSeconds = 600,
        double durationInTrafficSeconds = 900)
    {
        return new RouteDto
        {
            Legs =
            [
                new LegDto
                {
                    Distance = new TextValueDto { Value = distanceInMeters },
                    Duration = new TextValueDto { Value = durationInSeconds },
                    DurationInTraffic = new TextValueDto { Value = durationInTrafficSeconds }
                }
            ]
        };
    }

    public static RouteDto CreateMultiLegRouteDto(params (double distance, double duration)[] legs)
    {
        var legDtos = legs.Select(leg => new LegDto
        {
            Distance = new TextValueDto { Value = leg.distance },
            Duration = new TextValueDto { Value = leg.duration }
        }).ToList();

        return new RouteDto
        {
            Legs = legDtos
        };
    }

    public static RouteDto CreateEmptyRouteDto()
    {
        return new RouteDto
        {
            Legs = []
        };
    }

    public static RouteDto CreateNullLegsRouteDto()
    {
        return new RouteDto
        {
            Legs = null
        };
    }
}

