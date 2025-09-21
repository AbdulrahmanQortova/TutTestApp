using TutBackend.Data;
using Tut.Common.Models;

namespace TutBackend.Repositories;

public class UserRepository(TutDbContext context) : Repository<User>(context), IUserRepository;

