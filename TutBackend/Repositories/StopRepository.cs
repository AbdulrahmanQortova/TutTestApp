using TutBackend.Data;
using Tut.Common.Models;

namespace TutBackend.Repositories;

public class StopRepository(TutDbContext context) : Repository<Stop>(context), IStopRepository;

