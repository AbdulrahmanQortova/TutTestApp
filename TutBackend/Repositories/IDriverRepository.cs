using Tut.Common.Models;

namespace TutBackend.Repositories;

public interface IDriverRepository : IRepository<Driver>
{
    Task<Driver?> GetByIdDetailedAsync(int id);
    Task<Driver?> GetByMobileAsync(string mobile);
    Task<List<Driver>> GetByIdsAsync(IEnumerable<int> ids);
    Task<List<Driver>> GetAllDriversAsync();
}
