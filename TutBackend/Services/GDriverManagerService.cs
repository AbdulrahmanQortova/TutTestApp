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
        logger.LogInformation("Adding driver: {DriverFullName}", driver.FullName);
        logger.LogDebug("{Driver}", driver.ToJson());

        try
        {
            HttpResponseMessage resp = await qipClient.RegisterAsync(new RegisterRequest
            {
                Username = driver.Mobile,
                Password = driver.Password,
                Role = "Driver"
            });
            resp.EnsureSuccessStatusCode();
            await driverRepository.AddAsync(driver);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding driver");
            throw new RpcException(new Status(StatusCode.Internal, $"Error adding driver", ex));
        }
        GIdResponse response = new GIdResponse { Id = driver.Id };
        logger.LogDebug("Added Driver, Response= {Response}", response.ToJson());
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
    
    public async Task<DriverList> GetAllDrivers()
    {
        return new DriverList(await driverRepository.GetAllAsync());
    }
}
