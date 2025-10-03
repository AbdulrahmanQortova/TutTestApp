using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc;
using Tut.Common.Models;


namespace Tut.Common.GServices;

[Service]
public interface IGDriverLocationService
{
    public Task RegisterLocation(IAsyncEnumerable<GLocation> locations, CallContext context = default);
    public Task<List<DriverLocation>> GetDriverLocations();
    public Task<List<DriverLocation>> GetLocationHistoryForDriver(GIdRequest request);
}
