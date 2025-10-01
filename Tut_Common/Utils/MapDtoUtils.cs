using Tut.Common.Dto.MapDtos;
namespace Tut.Common.Utils;

public class MapDtoUtils
{
    public static double GetRouteDistance(DirectionResponseDto directionDto)
    {
        if (directionDto.Routes == null) return 0;
        if (directionDto.Routes.Count == 0) return 0;
        RouteDto route = directionDto.Routes[0];
        if (route.Legs == null) return 0;
        if (route.Legs.Count == 0) return 0;
        double distance = 0;
        foreach (LegDto routeLeg in route.Legs)
        {
            if (routeLeg.Distance?.Value is not null)
                distance += routeLeg.Distance.Value.Value;
        }
        return distance;
    }

    public static double GetRouteTime(DirectionResponseDto directionDto)
    {
        if (directionDto.Routes == null) return 0;
        if (directionDto.Routes.Count == 0) return 0;
        RouteDto route = directionDto.Routes[0];
        if (route.Legs == null) return 0;
        if (route.Legs.Count == 0) return 0;
        double time = 0;
        foreach (LegDto routeLeg in route.Legs)
        {
            if (routeLeg.DurationInTraffic?.Value is not null)
                time += routeLeg.DurationInTraffic.Value.Value;
            else if (routeLeg.Duration?.Value is not null)
                time += routeLeg.Duration.Value.Value;
        }

        return time;
    }

}
