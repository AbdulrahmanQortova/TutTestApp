using Tut.Common.Models;
using Tut.Common.Utils;
using TutBackend.Repositories;

namespace TutBackend.Services;

public class DriverSelector
{
    private readonly Func<GLocation, GLocation, double> _costFunction;
    private readonly ILogger<DriverSelector>? _logger;

    public DriverSelector(ILogger<DriverSelector>? logger = null, Func<GLocation, GLocation, double>? costFunction = null)
    {
        _logger = logger;
        _costFunction = costFunction ?? LocationUtils.DistanceInMeters;
    }

    public async Task<Driver?> FindBestDriverAsync(Trip trip, int[] excludedIds, IDriverLocationRepository driverLocationRepository, IDriverRepository driverRepository)
    {
        if (trip is null) throw new ArgumentNullException(nameof(trip));
        if (driverLocationRepository is null) throw new ArgumentNullException(nameof(driverLocationRepository));
        if (driverRepository is null) throw new ArgumentNullException(nameof(driverRepository));

        GLocation? pickup = trip.Stops[0].Place?.Location;
        if (pickup is null)
        {
            _logger?.LogError("Found a Trip without a Pickup:\n {Trip}", trip.ToJson());
            return null;
        }

        // Get latest known driver locations
        var locations = await driverLocationRepository.GetLatestDriverLocations();
        if (locations.Count == 0)
        {
            _logger?.LogWarning("No driver locations available when finding best driver for trip {TripId}", trip.Id);
            return null;
        }

        // Prepare excluded set for fast lookups
        var excludedSet = new HashSet<int>(excludedIds);

        // Build candidate id list from locations, excluding excluded ids
        var candidateIds = locations
            .Select(l => l.DriverId)
            .Where(id => !excludedSet.Contains(id))
            .Distinct()
            .ToList();

        if (candidateIds.Count == 0)
        {
            _logger?.LogInformation("No candidate drivers after applying excluded ids for trip {TripId}", trip.Id);
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
                _logger?.LogDebug("No driver record found for driverId {DriverId}", loc.DriverId);
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
                _logger?.LogError(ex, "Error computing cost for driver {DriverId}", driver.Id);
                continue;
            }

            if (cost >= bestCost) continue;
            bestCost = cost;
            bestDriver = driver;
        }

        if (bestDriver is null)
            _logger?.LogInformation("No available drivers found for trip {TripId}", trip.Id);

        return bestDriver;
    }
}

