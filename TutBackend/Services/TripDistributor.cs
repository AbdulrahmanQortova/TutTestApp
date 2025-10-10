using Tut.Common.Models;
using TutBackend.Repositories;

namespace TutBackend.Services;

public class TripDistributor(
    IServiceProvider services, 
    DriverSelector driverSelector,
    ILogger<TripDistributor> logger 
    ) 
    : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await DistributionLoop(stoppingToken);
    }

    private async Task DistributionLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var tripRepository = scope.ServiceProvider.GetRequiredService<ITripRepository>();
                var driverRepository = scope.ServiceProvider.GetRequiredService<IDriverRepository>();

                // Fetch a single unassigned trip directly from the repository.
                var trip = await tripRepository.GetOneUnassignedTripAsync();

                if (trip is null)
                {
                    // Nothing to do right now - wait and retry
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    continue;
                }

                logger.LogInformation("Found unassigned trip {TripId}, finding best driver...", trip.Id);

                // Use the injected DriverSelector to pick the best driver
                var bestDriver = await driverSelector.FindBestDriverAsync(trip, new HashSet<int>());
                if (bestDriver is null)
                {
                    logger.LogInformation("No suitable driver found for trip {TripId}", trip.Id);
                    // Wait a bit before retrying so we don't tight-loop on the same trip
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    continue;
                }
                bestDriver = await driverRepository.GetByIdAsync(bestDriver.Id);

                // Assign driver and persist
                trip.Driver = bestDriver;
                bestDriver!.State = DriverState.Requested;
                await driverRepository.UpdateAsync(bestDriver);
                await tripRepository.UpdateAsync(trip);
                logger.LogInformation("Assigned driver {DriverId} to trip {TripId}", bestDriver.Id, trip.Id);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Graceful shutdown requested
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in DistributionLoop");
                // Backoff on error
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
