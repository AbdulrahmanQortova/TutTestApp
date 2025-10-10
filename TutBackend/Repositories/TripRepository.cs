using Microsoft.EntityFrameworkCore;
using TutBackend.Data;
using Tut.Common.Models;

namespace TutBackend.Repositories;

public class TripRepository(TutDbContext context) : Repository<Trip>(context), ITripRepository
{
    public async Task<List<Trip>> GetAllTripsAsync(int take = 50, int skip = 0)
    {
        return await _dbSet
            .Include(t => t.User)
            .Include(t => t.Driver)
            .Include(t => t.RequestedDriverPlace)
            .Include(t => t.RequestingPlace)
            .Include(t => t.Stops)
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<Trip>> GetActiveTripsAsync(int take = 50, int skip = 0)
    {
        return await _dbSet
            .Include(t => t.User)
            .Include(t => t.Driver)
            .Include(t => t.RequestedDriverPlace)
            .Include(t => t.RequestingPlace)
            .Include(t => t.Stops)
            .Where(t => t.Status != TripState.Unspecified && t.Status != TripState.Ended && t.Status != TripState.Canceled)
            .OrderBy(t => t.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<Trip>> GetTripsForUser(int userId, int take = 50, int skip = 0)
    {
        return await _dbSet
            .Include(t => t.User)
            .Include(t => t.Driver)
            .Include(t => t.RequestedDriverPlace)
            .Include(t => t.RequestingPlace)
            .Include(t => t.Stops)
            .Where(t => t.User != null && t.User.Id == userId)
            .OrderBy(t => t.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        
    }
    public async Task<List<Trip>> GetTripsForDriver(int driverId, int take = 50, int skip = 0)
    {
        return await _dbSet
            .Include(t => t.User)
            .Include(t => t.Driver)
            .Include(t => t.RequestedDriverPlace)
            .Include(t => t.RequestingPlace)
            .Include(t => t.Stops)
            .Where(t => t.Driver != null && t.Driver.Id == driverId)
            .OrderBy(t => t.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Trip?> GetActiveTripForUser(int userId)
    {
        return await _dbSet
            .Include(t => t.User)
            .Include(t => t.Driver)
            .Include(t => t.RequestedDriverPlace)
            .Include(t => t.RequestingPlace)
            .Include(t => t.Stops)
            .SingleOrDefaultAsync(t => t.User != null && t.User.Id == userId && t.Status != TripState.Unspecified && t.Status != TripState.Ended && t.Status != TripState.Canceled);
    }
    public async Task<Trip?> GetActiveTripForDriver(int driverId)
    {
        return await _dbSet
            .Include(t => t.User)
            .Include(t => t.Driver)
            .Include(t => t.RequestedDriverPlace)
            .Include(t => t.RequestingPlace)
            .Include(t => t.Stops)
            .SingleOrDefaultAsync(t => t.Driver != null && t.Driver.Id == driverId && t.Status != TripState.Unspecified && t.Status != TripState.Ended && t.Status != TripState.Canceled);
    }

    // Returns a single trip that has not yet been assigned to a driver (Driver == null).
    public async Task<Trip?> GetOneUnassignedTripAsync()
    {
        return await _dbSet
            .Include(t => t.User)
            .Include(t => t.Driver)
            .Include(t => t.RequestedDriverPlace)
            .Include(t => t.RequestingPlace)
            .Include(t => t.Stops)
            .Where(t => t.Driver == null)
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }
    
}
