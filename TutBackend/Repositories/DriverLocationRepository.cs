using Microsoft.EntityFrameworkCore;
using TutBackend.Data;
using Tut.Common.Models;

namespace TutBackend.Repositories;

public class DriverLocationRepository(TutDbContext context) : Repository<DriverLocation>(context), IDriverLocationRepository
{
    public async Task<List<DriverLocation>> GetLatestDriverLocations()
    {
        return await _dbSet.Include(dl => dl.Location)
            .GroupBy(dl => dl.DriverId)
            .Select(group => group.OrderByDescending(dl => dl.Location.Timestamp).FirstOrDefault()!)
            .ToListAsync();
    }

    public async Task<List<DriverLocation>> GetLocationHistoryForDriver(int driverId, DateTime since)
    {
        return await _dbSet.Include(dl => dl.Location)
            .Where(dl => dl.Location.Timestamp > since && dl.DriverId == driverId)
            .OrderByDescending(dl => dl.Location.Timestamp)
            .ToListAsync();
    }
}

