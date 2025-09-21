using ProtoBuf.Grpc.Configuration;
using Tut.Common.Models;
namespace Tut.Common.GServices;

[Service]
public interface IGDriverManagerService
{
    public Task<GIdResponse> AddDriver(Driver driver);
    public Task<Driver> GetDriverById(GIdRequest request);
    public Task<Driver?> GetDriverByMobile(GStringRequest request);
    public Task DeleteDriver(GIdRequest request);
    public Task UpdateDriver(Driver driver);
    public Task<List<Driver>> GetAllDrivers();
}
