using ProtoBuf.Grpc.Configuration;
using Tut.Common.Models;


namespace Tut.Common.GServices;

[Service]
public interface IGDriverLocationService
{
    public Task RegisterLocation(IAsyncEnumerable<DriverLocation> locations);
    public Task<List<DriverLocation>> GetDriverLocations();
    public Task<List<DriverLocation>> GetLocationHistoryForDriver(GIdRequest request);
}
