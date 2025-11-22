using Tut.Common.Models;
namespace TutBackend.Services;

public static class DriverCache
{
    private static readonly Dictionary<int, DriverState> DriverStates = [];
    private static readonly Dictionary<int, DriverLocation> DriverLocations = [];

    public static void SetDriverState(int driverId, DriverState state)
    {
        DriverStates[driverId] = state;
    }
    public static DriverState GetDriverState(int driverId)
    {
        return DriverStates.GetValueOrDefault(driverId, DriverState.Unspecified);
    }
    public static void SetDriverLocation(int driverId, DriverLocation location)
    {
        DriverLocations[driverId] = location;
    }
    public static DriverLocation? GetDriverLocation(int driverId)
    {
        DriverLocations.TryGetValue(driverId, out DriverLocation? location);
        return location;
    }

    public static List<DriverLocation> GetDriverLocations()
    {
        return DriverLocations.Values.ToList();
    }
}
