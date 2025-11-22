using Microsoft.EntityFrameworkCore;
using TutBackend.Data;
using Tut.Common.Models;
using TutBackend.Services;

namespace TutBackend.Repositories;

public class DriverLocationRepository(TutDbContext context) : Repository<DriverLocation>(context), IDriverLocationRepository
{
    private readonly Dictionary<int, DateTime> _lastStoredTimestamps = [];
    private const int MinSecondsBetweenStores = 60*15; // 15 minutes
    public Task<List<DriverLocation>> GetLatestDriverLocations()
    {
        return Task.FromResult(DriverCache.GetDriverLocations());
    }

    public Task<DriverLocation?> GetLatestDriverLocation(int id)
    {
        return Task.FromResult(DriverCache.GetDriverLocation(id));
    }
    
    public async Task<List<DriverLocation>> GetLocationHistoryForDriver(int driverId, DateTime since)
    {
        return await _dbSet
            .Where(dl => dl.Timestamp > since && dl.DriverId == driverId)
            .OrderByDescending(dl => dl.Timestamp)
            .ThenByDescending(dl => dl.Id)
            .ToListAsync();
    }
    
    public async Task SaveDriverLocationAsync(DriverLocation location)
    {
        DriverCache.SetDriverLocation(location.DriverId, location);
        if(_lastStoredTimestamps.TryGetValue(location.DriverId, out var lastStored)
           && (location.Timestamp - lastStored).TotalSeconds < MinSecondsBetweenStores)
        {
            // Skip storing to DB too frequently
            return;
        }
        await AddAsync(location);
        _lastStoredTimestamps[location.DriverId] = location.Timestamp;
    }

}

