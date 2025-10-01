using Tut.Common.Models;

namespace TutBackend.Repositories;

public interface ITripRepository : IRepository<Trip>
{
    Task<List<Trip>> GetAllTripsAsync(int take = 50, int skip = 0);
    Task<List<Trip>> GetActiveTripsAsync(int take = 50, int skip = 0);
    Task<List<Trip>> GetTripsForUser(int userId, int take = 50, int skip = 0);
    Task<List<Trip>> GetTripsForDriver(int driverId, int take = 50, int skip = 0);
    Task<Trip?> GetActiveTripForUser(int userId);
    Task<Trip?> GetActiveTripForDriver(int driverId);
}

