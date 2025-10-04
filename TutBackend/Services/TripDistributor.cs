using Tut.Common.Models;
using TutBackend.Repositories;

namespace TutBackend.Services;

public class TripDistributor : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TripDistributor> _logger;
    private readonly DriverSelector _driverSelector;

    public TripDistributor(IServiceProvider services, ILogger<TripDistributor> logger, DriverSelector driverSelector)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _driverSelector = driverSelector ?? throw new ArgumentNullException(nameof(driverSelector));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await DistributionLoop(stoppingToken).ConfigureAwait(false);
    }

    private async Task DistributionLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var tripRepository = scope.ServiceProvider.GetRequiredService<ITripRepository>();
                var driverLocationRepository = scope.ServiceProvider.GetRequiredService<IDriverLocationRepository>();
                var driverRepository = scope.ServiceProvider.GetRequiredService<IDriverRepository>();

                // Fetch a single unassigned trip directly from the repository.
                var trip = await tripRepository.GetOneUnassignedTripAsync();

                if (trip is null)
                {
                    // Nothing to do right now - wait and retry
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    continue;
                }

                _logger.LogInformation("Found unassigned trip {TripId}, finding best driver...", trip.Id);

                // Use the injected DriverSelector to pick the best driver
                var bestDriver = await _driverSelector.FindBestDriverAsync(trip, [], driverLocationRepository, driverRepository).ConfigureAwait(false);
                if (bestDriver is null)
                {
                    _logger.LogInformation("No suitable driver found for trip {TripId}", trip.Id);
                    // Wait a bit before retrying so we don't tight-loop on the same trip
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    continue;
                }

                // Assign driver and persist
                trip.Driver = bestDriver;
                bestDriver.State = DriverState.Requested;
                await driverRepository.UpdateAsync(bestDriver);
                await tripRepository.UpdateAsync(trip);
                _logger.LogInformation("Assigned driver {DriverId} to trip {TripId}", bestDriver.Id, trip.Id);

                // Small delay before processing next trip to avoid DB hot loop
                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Graceful shutdown requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DistributionLoop");
                // Backoff on error
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
