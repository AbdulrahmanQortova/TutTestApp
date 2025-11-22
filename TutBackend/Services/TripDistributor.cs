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
    private readonly TimeSpan _delayBetweenIterations = TimeSpan.FromMilliseconds(200);
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
                    await Task.Delay(_delayBetweenIterations, cancellationToken);
                    continue;
                }

                logger.LogInformation("Found unassigned trip {TripId}, finding best driver...", trip.Id);

                // Use the injected DriverSelector to pick the best driver
                int bestDriverId = await driverSelector.FindBestDriverIdAsync(trip, new HashSet<int>());
                if (bestDriverId < 0)
                {
                    logger.LogInformation("No suitable driver found for trip {TripId}", trip.Id);
                    // Wait a bit before retrying so we don't tight-loop on the same trip
                    await Task.Delay(_delayBetweenIterations, cancellationToken);
                    continue;
                }
                Driver? bestDriver = await driverRepository.GetByIdAsync(bestDriverId);
                if(bestDriver is null)
                {
                    logger.LogInformation("No driver found with specified Id {DriverId}", bestDriverId);
                    // Wait a bit before retrying so we don't tight-loop on the same trip
                    await Task.Delay(_delayBetweenIterations, cancellationToken);
                    continue;
                }

                // Assign driver and persist
                trip.Driver = bestDriver;
                await driverRepository.SetDriverStateAsync(bestDriver, DriverState.Requested);
                await tripRepository.UpdateAsync(trip);
                logger.LogInformation("Assigned driver {DriverId} to trip {TripId}", bestDriver.Id, trip.Id);
                await Task.Delay(_delayBetweenIterations, cancellationToken);
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
