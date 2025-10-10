using Microsoft.EntityFrameworkCore;
using TutBackend.Data;
using Tut.Common.Models;

namespace TutBackend.Repositories;

public class DriverLocationRepository(TutDbContext context) : Repository<DriverLocation>(context), IDriverLocationRepository
{
    public async Task<List<DriverLocation>> GetLatestDriverLocations()
    {
        return await _dbSet
            .GroupBy(dl => dl.DriverId)
            .Select(group => group
                .OrderByDescending(dl => dl.Timestamp)
                .ThenByDescending(dl => dl.Id)
                .FirstOrDefault()!)
            .ToListAsync();
    }

    public async Task<DriverLocation?> GetLatestDriverLocation(int id)
    {
        return await _dbSet
            .OrderByDescending(dl => dl.Timestamp)
            .ThenByDescending(dl => dl.Id)
            .FirstOrDefaultAsync();
    }
    
    public async Task<List<DriverLocation>> GetLocationHistoryForDriver(int driverId, DateTime since)
    {
        return await _dbSet
            .Where(dl => dl.Timestamp > since && dl.DriverId == driverId)
            .OrderByDescending(dl => dl.Timestamp)
            .ThenByDescending(dl => dl.Id)
            .ToListAsync();
    }

}

