using Microsoft.EntityFrameworkCore;
using TutBackend.Data;
using Tut.Common.Models;
using TutBackend.Services;

namespace TutBackend.Repositories;

public class DriverRepository(TutDbContext context) : Repository<Driver>(context), IDriverRepository
{
    public async Task<Driver?> GetByIdDetailedAsync(int id)
    {
        // Load driver with full related graph: trips, each trip's user and stops.
        return await _dbSet
            .Where(d => d.Id == id)
            .Include(d => d.Trips!)
                .ThenInclude(t => t.User)
            .Include(d => d.Trips!)
                .ThenInclude(t => t.Stops)
            .SingleOrDefaultAsync();
    }

    public async Task<Driver?> GetByMobileAsync(string mobile)
    {
        return await _dbSet.SingleOrDefaultAsync(d => d.Mobile == mobile);
    }

    // New: fetch multiple drivers by ids in a single query
    public async Task<List<Driver>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var idList = ids.Where(i => i > 0).Distinct().ToList();
        if (idList.Count == 0)
            return [];
        return await _dbSet.Where(d => idList.Contains(d.Id)).ToListAsync();
    }

    public async Task<List<Driver>> GetAllDriversAsync()
    {
        var lst = _dbSet.Select(d => new
        {
            Driver = d,
            TotalTrips = d.Trips!.Count,
            TotalEarnings = d.Trips.Sum(t => t.ActualCost)
        });
        await lst.ForEachAsync(dt =>
        {
            dt.Driver.TotalTrips = dt.TotalTrips;
            dt.Driver.TotalEarnings = dt.TotalEarnings;
        });
        return await lst.Select(dt => dt.Driver).ToListAsync();
    }
    
    public async Task SetDriverStateAsync(int driverId, DriverState state)
    {
        var driver = await _dbSet.FindAsync(driverId);
        if (driver is null)
            throw new KeyNotFoundException($"Driver with Id {driverId} not found.");
        await SetDriverStateAsync(driver, state);
    }
    
    public async Task SetDriverStateAsync(Driver driver, DriverState state)
    {
        DriverCache.SetDriverState(driver.Id, state);
        driver.State = state;
        _context.Update(driver);
        await _context.SaveChangesAsync();
    }
}
