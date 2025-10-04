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
            .Include(t => t.RequestedDriverPlace).ThenInclude(p => p!.Location)
            .Include(t => t.RequestingPlace).ThenInclude(p => p!.Location)
            .Include(t => t.Stops).ThenInclude(s => s.Place).ThenInclude(p => p!.Location)
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
            .Include(t => t.RequestedDriverPlace).ThenInclude(p => p!.Location)
            .Include(t => t.RequestingPlace).ThenInclude(p => p!.Location)
            .Include(t => t.Stops).ThenInclude(s => s.Place).ThenInclude(p => p!.Location)
            .Where(t => t.Status != TripState.Unspecified && t.Status == TripState.Ended && t.Status == TripState.Canceled)
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
            .Include(t => t.RequestedDriverPlace).ThenInclude(p => p!.Location)
            .Include(t => t.RequestingPlace).ThenInclude(p => p!.Location)
            .Include(t => t.Stops).ThenInclude(s => s.Place).ThenInclude(p => p!.Location)
            .Where(t => t.User.Id == userId)
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
            .Include(t => t.RequestedDriverPlace).ThenInclude(p => p!.Location)
            .Include(t => t.RequestingPlace).ThenInclude(p => p!.Location)
            .Include(t => t.Stops).ThenInclude(s => s.Place).ThenInclude(p => p!.Location)
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
            .Include(t => t.RequestedDriverPlace).ThenInclude(p => p!.Location)
            .Include(t => t.RequestingPlace).ThenInclude(p => p!.Location)
            .Include(t => t.Stops).ThenInclude(s => s.Place).ThenInclude(p => p!.Location)
            .SingleOrDefaultAsync(t => t.User.Id == userId && t.Status != TripState.Unspecified && t.Status == TripState.Ended && t.Status == TripState.Canceled);
    }
    public async Task<Trip?> GetActiveTripForDriver(int driverId)
    {
        return await _dbSet
            .Include(t => t.User)
            .Include(t => t.Driver)
            .Include(t => t.RequestedDriverPlace).ThenInclude(p => p!.Location)
            .Include(t => t.RequestingPlace).ThenInclude(p => p!.Location)
            .Include(t => t.Stops).ThenInclude(s => s.Place).ThenInclude(p => p!.Location)
            .SingleOrDefaultAsync(t => t.Driver != null && t.Driver.Id == driverId && t.Status != TripState.Unspecified && t.Status == TripState.Ended && t.Status == TripState.Canceled);
    }
    
}
