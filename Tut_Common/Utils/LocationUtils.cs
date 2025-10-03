using Tut.Common.Models;
namespace Tut.Common.Utils;

public static class LocationUtils
{
    public static double DistanceInKm(GLocation src, GLocation dst)
    {
        double x = dst.Latitude - src.Latitude;
        double y = (dst.Longitude - src.Longitude) * Math.Cos((dst.Latitude + src.Latitude) * 0.00872664626);
        return 111.319 * Math.Sqrt(x * x + y * y);
    }

    public static double DistanceInMeters(GLocation src, GLocation dst)
    {
        return DistanceInKm(src, dst) * 1000;
    }

    public static GLocation Towards(GLocation src, GLocation dst, double distance)
    {
        // Non-positive distance -> return source
        if (distance <= 0)
        {
            return new GLocation { Latitude = src.Latitude, Longitude = src.Longitude };
        }

        double totalMeters = DistanceInMeters(src, dst);

        // If src and dst are the same point or distance to move is >= total, return dst
        if (totalMeters <= 0 || distance >= totalMeters)
        {
            return new GLocation { Latitude = dst.Latitude, Longitude = dst.Longitude };
        }

        // Linear interpolation by fraction of the total distance.
        // This is consistent with the approximate planar distance used above.
        double fraction = distance / totalMeters;
        double lat = src.Latitude + (dst.Latitude - src.Latitude) * fraction;
        double lon = src.Longitude + (dst.Longitude - src.Longitude) * fraction;

        return new GLocation { Latitude = lat, Longitude = lon };
    }

    /// <summary>
    /// Determines if two locations are effectively the same allowing for some error 
    /// </summary>
    /// <param name="a">First Location</param>
    /// <param name="b">Second Location</param>
    /// <param name="error">Error in meters</param>
    /// <returns></returns>
    public static bool SameLocation(GLocation a, GLocation b, double error = 20)
    {
        return DistanceInMeters(a,b) < error;
    }
}
