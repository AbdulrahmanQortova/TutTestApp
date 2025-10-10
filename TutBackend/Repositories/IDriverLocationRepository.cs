using Tut.Common.Models;

namespace TutBackend.Repositories;

public interface IDriverLocationRepository : IRepository<DriverLocation>
{
    Task<List<DriverLocation>> GetLatestDriverLocations();
    Task<List<DriverLocation>> GetLocationHistoryForDriver(int driverId, DateTime since);
    Task<DriverLocation?> GetLatestDriverLocation(int id);
}

