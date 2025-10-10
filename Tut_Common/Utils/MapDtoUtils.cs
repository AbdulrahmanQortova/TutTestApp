using Tut.Common.Dto.MapDtos;
namespace Tut.Common.Utils;

public class MapDtoUtils
{
    public static double GetRouteDistance(RouteDto routeDto)
    {
        if (routeDto.Legs == null) return 0;
        if (routeDto.Legs.Count == 0) return 0;
        double distance = 0;
        routeDto.Legs.ForEach(routeLeg =>
        {
            if (routeLeg.Distance?.Value is not null)
                distance += routeLeg.Distance.Value.Value;
        });
        return distance;
    }

    public static double GetRouteTime(RouteDto routeDto)
    {
        if (routeDto.Legs == null) return 0;
        if (routeDto.Legs.Count == 0) return 0;
        double time = 0;
        foreach (LegDto routeLeg in routeDto.Legs)
        {
            if (routeLeg.DurationInTraffic?.Value is not null)
                time += routeLeg.DurationInTraffic.Value.Value;
            else if (routeLeg.Duration?.Value is not null)
                time += routeLeg.Duration.Value.Value;
        }
        return time;
    }
}
