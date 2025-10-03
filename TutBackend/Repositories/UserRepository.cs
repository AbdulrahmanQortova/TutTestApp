using Microsoft.EntityFrameworkCore;
using TutBackend.Data;
using Tut.Common.Models;

namespace TutBackend.Repositories;

public class UserRepository(TutDbContext context) : Repository<User>(context), IUserRepository
{


    public async Task<User?> GetByMobileAsync(string mobile)
    {
        return await _dbSet.SingleOrDefaultAsync(u => u.Mobile == mobile);
    }
}

