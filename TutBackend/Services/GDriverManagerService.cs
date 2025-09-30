using Grpc.Core;
using Tut.Common.GServices;
using Tut.Common.Models;
using Tut.Common.Utils;
using TutBackend.Repositories;
namespace TutBackend.Services;

public class GDriverManagerService(IDriverRepository driverRepository, QipClient qipClient, ILogger<GDriverManagerService> logger) : IGDriverManagerService
{

    public async Task<GIdResponse> AddDriver(Driver driver)
    {
        logger.LogInformation("Adding driver: {DriverFirstName} {DriverLastName}", driver.FirstName, driver.LastName);
        logger.LogDebug("{Driver}", driver.ToJson());
        GIdResponse response = new ();

        try
        {
            await driverRepository.AddAsync(driver);
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, $"Error adding driver", ex));
        }
        
        logger.LogDebug("{Response}", response.ToJson());
        return response;
    }
    
    
    public async Task<Driver> GetDriverById(GIdRequest request)
    {
        Driver? driver = await driverRepository.GetByIdAsync(request.Id);
        return driver ?? throw new RpcException(new Status(StatusCode.NotFound, $"Driver not found with id: {request.Id}"));
    }
    
    public async Task<Driver?> GetDriverByMobile(GStringRequest request)
    {
        return await driverRepository.GetByMobileAsync(request.Arg);
    }
    
    public Task DeleteDriver(GIdRequest request)
    {
        throw new NotImplementedException();
    }
    public async Task UpdateDriver(Driver driver)
    {
        await driverRepository.UpdateAsync(driver);
    }
    
    public async Task<List<Driver>> GetAllDrivers()
    {
        return [..await driverRepository.GetAllAsync()];
    }
}
