using Tut.Common.Models;
using Tut.Common.Utils;
using TutBackend.Repositories;
namespace TutBackend.Services;

public class TripDistributor (
    ITripRepository tripRepository,
    IDriverLocationRepository driverLocationRepository,
    IDriverRepository driverRepository,
    ILogger<TripDistributor> logger
    )
{


    private async Task DistributionLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            
        }
    }

    private async Task<Driver?> FindBestDriver(Trip trip, int[] excludedIds)
    {
        GLocation? pickup = trip.Stops[0].Place?.Location;
        if (pickup is null)
        {
            logger.LogError("Found a Trip without a Pickup:\n {Trip}", trip.ToJson());
            return null;
        }

        // Get latest known driver locations
        var locations = await driverLocationRepository.GetLatestDriverLocations();
        if (locations.Count == 0)
        {
            logger.LogWarning("No driver locations available when finding best driver for trip {TripId}", trip.Id);
            return null;
        }

        // Prepare excluded set for fast lookups
        var excludedSet = new HashSet<int>(excludedIds ?? Array.Empty<int>());

        // Build candidate id list from locations, excluding excluded ids
        var candidateIds = locations
            .Select(l => l.DriverId)
            .Where(id => !excludedSet.Contains(id))
            .Distinct()
            .ToList();

        if (candidateIds.Count == 0)
        {
            logger.LogInformation("No candidate drivers after applying excluded ids for trip {TripId}", trip.Id);
            return null;
        }

        // Fetch drivers in one DB call
        var drivers = await driverRepository.GetByIdsAsync(candidateIds);
        var driverMap = drivers.ToDictionary(d => d.Id);

        Driver? bestDriver = null;
        double bestCost = double.PositiveInfinity;

        foreach (var loc in locations)
        {
            // Skip excluded drivers
            if (excludedSet.Contains(loc.DriverId))
                continue;

            // Try get driver from the batch result
            if (!driverMap.TryGetValue(loc.DriverId, out var driver))
            {
                logger.LogDebug("No driver record found for driverId {DriverId}", loc.DriverId);
                continue;
            }

            // Only consider available drivers
            if (driver.State != DriverState.Available)
                continue;

            // Compute cost using driver's reported location (from the location repository)
            var driverLocation = loc.Location;
            double cost;
            try
            {
                cost = _costFunction(pickup, driverLocation);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error computing cost for driver {DriverId}", driver.Id);
                continue;
            }

            if (cost >= bestCost) continue;
            bestCost = cost;
            bestDriver = driver;
        }

        if (bestDriver is null)
            logger.LogInformation("No available drivers found for trip {TripId}", trip.Id);

        return bestDriver;
    }

    private readonly Func<GLocation, GLocation, double> _costFunction = CartesianDistance;


    private static readonly Func<GLocation, GLocation, double> CartesianDistance = LocationUtils.DistanceInMeters;
}
