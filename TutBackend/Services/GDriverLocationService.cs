using Grpc.Core;
using ProtoBuf.Grpc;
using Tut.Common.Business;
using Tut.Common.GServices;
using Tut.Common.Models;
using Tut.Common.Utils;
using TutBackend.Data;
using TutBackend.Repositories;
namespace TutBackend.Services;

public class GDriverLocationService(IDriverLocationRepository driverLocationRepository
    , IDriverRepository driverRepository
    , QipClient qipClient
    , TutDbContext dbContext
    , ILogger<GDriverManagerService> logger)
    : IGDriverLocationService
{

    private Driver? _driver;
    
    public async Task RegisterLocation(IAsyncEnumerable<GLocation> locations, CallContext context = default)
    {
        _driver = await AuthUtils.AuthorizeDriver(context, driverRepository, qipClient);
        if (_driver is null)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthorized"));
        try
        {
            await foreach (GLocation location in locations)
            {
                await dbContext.Entry(_driver).ReloadAsync();
                _driver = await driverRepository.GetByIdAsync(_driver.Id);
                if (_driver is null)
                    throw new RpcException(new Status(StatusCode.NotFound, "Driver not found in Database"));
                await driverLocationRepository.SaveDriverLocationAsync(new DriverLocation
                {
                    DriverId = _driver.Id,
                    DriverName = _driver.FullName,
                    DriverState = _driver.State,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    Altitude = location.Altitude,
                    Course = location.Course,
                    Speed = location.Speed,
                    Timestamp = location.Timestamp
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Exception in RegisterLocation:{Driver}", _driver);
        }
    }
    
    public async Task<DriverLocationList> GetDriverLocations()
    {
        return new DriverLocationList(await driverLocationRepository.GetLatestDriverLocations());
    }

    public async Task<DriverLocationList> GetLocationHistoryForDriver(GIdRequest request)
    {
        Driver? driver = await driverRepository.GetByIdAsync(request.Id);
        if(driver is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Driver not found with id: {request.Id}"));
        return new DriverLocationList(await driverLocationRepository.GetLocationHistoryForDriver(request.Id, DateTime.Now.AddDays(-3)));
    }
}
