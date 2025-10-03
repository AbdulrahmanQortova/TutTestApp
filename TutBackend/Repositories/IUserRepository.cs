using Tut.Common.Models;

namespace TutBackend.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByMobileAsync(string mobile);
}

