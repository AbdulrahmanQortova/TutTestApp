using ProtoBuf.Grpc.Configuration;
using Tut.Common.Models;
namespace Tut.Common.GServices;

[Service]
public interface IGTripManagerService
{
    Task<List<Trip>> GetAllTrips(GPartialListRequest request);
    Task<List<Trip>> GetAllActiveTrips(GPartialListRequest request);
    Task<List<Trip>> GetTripsForUser(GPartialListIdRequest request);
    Task<List<Trip>> GetTripsForDriver(GPartialListIdRequest request);
    Task<Trip?> GetActiveTripForUser(GIdRequest request);
    Task<Trip?> GetActiveTripForDriver(GIdRequest request);
}
