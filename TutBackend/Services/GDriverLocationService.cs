using Grpc.Core;
using Tut.Common.GServices;
using Tut.Common.Models;
using TutBackend.Repositories;
namespace TutBackend.Services;

public class GDriverLocationService(IDriverLocationRepository driverLocationRepository
    , IDriverRepository driverRepository
    , ILogger<GDriverManagerService> logger)
    : IGDriverLocationService
{

    public async Task RegisterLocation(IAsyncEnumerable<DriverLocation> locations)
    {
        int driverId = -1;
        try
        {
            await foreach (DriverLocation location in locations)
            {
                if (driverId < 0)
                {
                    
                }
                driverId = location.DriverId;
                await driverLocationRepository.AddAsync(location);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Exception in RegisterLocation:{DriverId}", driverId);
        }
    }
    
    public async Task<List<DriverLocation>> GetDriverLocations()
    {
        return await driverLocationRepository.GetLatestDriverLocations();
    }

    public async Task<List<DriverLocation>> GetLocationHistoryForDriver(GIdRequest request)
    {
        Driver? driver = await driverRepository.GetByIdAsync(request.Id);
        if(driver is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Driver not found with id: {request.Id}"));
        return await driverLocationRepository.GetLocationHistoryForDriver(request.Id, DateTime.Now.AddDays(-3));
    }
}
