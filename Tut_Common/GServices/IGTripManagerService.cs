using ProtoBuf.Grpc.Configuration;
using Tut.Common.Models;

namespace Tut.Common.GServices;

[ProtoBuf.Grpc.Configuration.Service]
public interface IGTripManagerService
{
    Task<TripList> GetAllTrips(GPartialListRequest request);
    Task<TripList> GetAllActiveTrips(GPartialListRequest request);
    Task<TripList> GetTripsForUser(GPartialListIdRequest request);
    Task<TripList> GetTripsForDriver(GPartialListIdRequest request);
    Task<Trip?> GetActiveTripForUser(GIdRequest request);
    Task<Trip?> GetActiveTripForDriver(GIdRequest request);
}
