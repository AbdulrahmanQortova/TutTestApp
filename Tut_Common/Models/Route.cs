using Tut.Common.Dto.MapDtos;
namespace Tut.Common.Models;

public class Route
{
    public List<LocationDto> Points { get; set; } = [];
    public string EncodedPoints { get; private set; } = string.Empty;
    
    public Route() { }

    public Route(RouteDto dto)
    {
        EncodedPoints = dto.OverviewPolyline?.Points ?? string.Empty;
        Points = DecodePolylinePoints(EncodedPoints) ?? [];
    }

    public Route(string routeString)
    {
        EncodedPoints = routeString;
        Points = DecodePolylinePoints(EncodedPoints) ?? [];
    }
    
    
    private static List<LocationDto>? DecodePolylinePoints(string? encodedPoints)
    {
        if (string.IsNullOrEmpty(encodedPoints))
            return null;
        var poly = new List<LocationDto>();
        char[] polylineChars = encodedPoints.ToCharArray();
        int index = 0;
        int currentLat = 0;
        int currentLng = 0;
        try
        {
            while (index < polylineChars.Length)
            {
                int latChange = DecodeNext(polylineChars, ref index);
                if (index >= polylineChars.Length && latChange == 0)
                    break;
                currentLat += latChange;

                int lngChange = DecodeNext(polylineChars, ref index);
                if (index >= polylineChars.Length && lngChange == 0)
                    break;
                currentLng += lngChange;

                LocationDto p = new()
                {
                    Lat = Convert.ToDouble(currentLat) / 100000.0,
                    Lng = Convert.ToDouble(currentLng) / 100000.0
                };
                poly.Add(p);
            }
        }
        catch
        {
            return poly;
        }
        return poly;
    }

    // Read next value from polyline char array (delta value)
    private static int DecodeNext(char[] chars, ref int index)
    {
        int sum = 0;
        int shifter = 0;
        while (index < chars.Length)
        {
            int next5Bits = chars[index++] - 63;
            sum |= (next5Bits & 31) << shifter;
            shifter += 5;
            if (next5Bits < 32)
                break;
        }

        return (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);
    }

}
