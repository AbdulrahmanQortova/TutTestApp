using Tut.Common.Models;
using Tut.Common.Utils;
using TutBackend.Repositories;

namespace TutBackend.Services;

public class DriverSelector
{
    private IServiceProvider _serviceProvider;
    private IDriverLocationRepository driverLocationRepository;
    private IDriverRepository driverRepository;
    private ILogger<DriverSelector> logger;
    
    public DriverSelector(IServiceProvider serviceProvider, ILogger<DriverSelector> logger)
    {
        _serviceProvider = serviceProvider;
        var scope = serviceProvider.CreateScope();
        driverLocationRepository = scope.ServiceProvider.GetRequiredService<IDriverLocationRepository>();
        driverRepository = scope.ServiceProvider.GetRequiredService<IDriverRepository>();
        this.logger = logger;

    }
    
    public async Task<int> FindBestDriverIdAsync(Trip trip, ISet<int> excludedIds, Func<GLocation, GLocation, double>? costFunction = null)
    {
        costFunction ??= LocationUtils.DistanceInMeters;
        if (trip.Stops.Count == 0)
        {
            logger?.LogError("Found a Trip without a Pickup:\n {Trip}", trip.ToJson());
            return -1;
        }
        
        Place pickup = trip.Stops[0];

        // Get latest known driver locations
        var locations = await driverLocationRepository.GetLatestDriverLocations();
        if (locations.Count == 0)
        {
            logger?.LogWarning("No driver locations available when finding best driver for trip {TripId}", trip.Id);
            return -1;
        }


        // Build candidate id list from locations, excluding excluded ids
        var candidateIds = locations
            .Select(l => l.DriverId)
            .Where(id => !excludedIds.Contains(id))
            .Distinct()
            .ToList();

        if (candidateIds.Count == 0)
        {
            logger?.LogInformation("No candidate drivers after applying excluded ids for trip {TripId}", trip.Id);
            return -1;
        }

        int bestDriverId = -1;
        double bestCost = double.PositiveInfinity;

        foreach (var loc in locations)
        {
            // Skip excluded drivers
            if (excludedIds.Contains(loc.DriverId))
                continue;

            // Only consider available drivers
            if (loc.DriverState != DriverState.Available)
                continue;

            // Compute cost using driver's reported location (from the location repository)
            double cost;
            try
            {
                cost = costFunction(pickup.ToLocation(), loc.ToLocation());
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error computing cost for driver {DriverId}", loc.DriverId);
                continue;
            }

            if (cost >= bestCost) continue;
            bestCost = cost;
            bestDriverId = loc.DriverId;
        }

        if (bestDriverId >= 0)
            return bestDriverId;
        
        logger?.LogInformation("No available drivers found for trip {TripId}", trip.Id);
        return -1;

    }

    public async Task<Driver?> FindBestDriverAsync(Trip trip, ISet<int> excludedIds, Func<GLocation, GLocation, double>? costFunction = null)
    {
        int bestDriverId = await FindBestDriverIdAsync(trip, excludedIds, costFunction);
        if (bestDriverId < 0) return null;
        return await driverRepository.GetByIdAsync(bestDriverId);
    }

    public async Task<int> EstimateDriverArrivalTime(Trip trip, Func<GLocation, GLocation, double>? costFunction = null)
    {
        costFunction ??= LocationUtils.DistanceInMeters;
        if (trip.Stops.Count == 0)
        {
            logger?.LogError("Found a Trip without a Pickup:\n {Trip}", trip.ToJson());
            return -1;
        }
        
        Place pickup = trip.Stops[0];

        // Get latest known driver locations
        var locations = await driverLocationRepository.GetLatestDriverLocations();
        if (locations.Count == 0)
        {
            logger?.LogWarning("No driver locations available when finding best driver for trip {TripId}", trip.Id);
            return -1;
        }

        double bestCost = double.PositiveInfinity;
        DriverLocation? bestLocation = null;


        foreach (var loc in locations)
        {
            // Only consider available drivers
            if (loc.DriverState != DriverState.Available)
                continue;

            // Compute cost using driver's reported location (from the location repository)
            double cost;
            try
            {
                cost = costFunction(pickup.ToLocation(), loc.ToLocation());
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error computing cost for driver {DriverId}", loc.DriverId);
                continue;
            }

            if (cost >= bestCost) continue;
            bestCost = cost;
            bestLocation = loc;
        }

        if (bestLocation is null) return 0;
        return (int)(LocationUtils.DistanceInMeters(pickup.ToLocation(), bestLocation.ToLocation()) * 1.5 / 50_000 * 60 * 60);
    }
}

