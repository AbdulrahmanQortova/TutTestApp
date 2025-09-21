using Microsoft.EntityFrameworkCore;
using TutBackend.Data;
using Tut.Common.Models;

namespace TutBackend.Repositories;

public class DriverRepository(TutDbContext context) : Repository<Driver>(context), IDriverRepository
{
    public async Task<Driver?> GetByIdDetailedAsync(int id)
    {
        // Load driver with full related graph: trips, each trip's user, requested/requesting places (with locations),
        // and stops with their places (and place locations).
        return await _dbSet
            .Where(d => d.Id == id)
            .Include(d => d.Location)
            .Include(d => d.Trips!)
                .ThenInclude(t => t.User)
            .Include(d => d.Trips!)
                .ThenInclude(t => t.RequestedDriverPlace!)
                    .ThenInclude(p => p.Location)
            .Include(d => d.Trips!)
                .ThenInclude(t => t.RequestingPlace!)
                    .ThenInclude(p => p.Location)
            .Include(d => d.Trips!)
                .ThenInclude(t => t.Stops)
                    .ThenInclude(s => s.Place!)
                        .ThenInclude(p => p.Location)
            .AsSplitQuery()
            .SingleOrDefaultAsync();
    }

    public async Task<Driver?> GetByMobileAsync(string mobile)
    {
        return await _dbSet.SingleOrDefaultAsync(d => d.Mobile == mobile);
    }
}
