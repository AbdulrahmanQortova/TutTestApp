using Grpc.Core;
using Tut.Common.GServices;
using Tut.Common.Models;
using TutBackend.Repositories;
namespace TutBackend.Services;

public class GTripManagerService(ITripRepository tripRepository,
    IUserRepository userRepository,
    IDriverRepository driverRepository
    )
    : IGTripManagerService
{

    public async Task<List<Trip>> GetAllTrips(GPartialListRequest request)
    {
        return await tripRepository.GetAllTripsAsync(request.Take, request.Skip);
    }
    public async Task<List<Trip>> GetAllActiveTrips(GPartialListRequest request)
    {
        return await tripRepository.GetActiveTripsAsync(request.Take, request.Skip);
    }
    public async Task<List<Trip>> GetTripsForUser(GPartialListIdRequest request)
    {
        User? user = await userRepository.GetByIdAsync(request.Id);
        if(user is null) 
            throw new RpcException(new Status(StatusCode.NotFound, $"User not found with id: {request.Id}"));
        return await tripRepository.GetTripsForUser(user.Id, request.Take, request.Skip);
    }
    public async Task<List<Trip>> GetTripsForDriver(GPartialListIdRequest request)
    {
        Driver? driver = await driverRepository.GetByIdAsync(request.Id);
        if(driver is null) 
            throw new RpcException(new Status(StatusCode.NotFound, $"Driver not found with id: {request.Id}"));
        return await tripRepository.GetTripsForDriver(driver.Id, request.Take, request.Skip);
    }
    public async Task<Trip?> GetActiveTripForUser(GIdRequest request)
    {
        User? user = await userRepository.GetByIdAsync(request.Id);
        if(user is null) 
            throw new NotImplementedException();
        return await tripRepository.GetActiveTripForUser(request.Id);
    }
    public async Task<Trip?> GetActiveTripForDriver(GIdRequest request)
    {
        Driver? driver = await driverRepository.GetByIdAsync(request.Id);
        if(driver is null) 
            throw new RpcException(new Status(StatusCode.NotFound, $"Driver not found with id: {request.Id}"));
        return await tripRepository.GetActiveTripForDriver(request.Id);
    }
}
