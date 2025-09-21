using TutBackend.Data;
using Tut.Common.Models;

namespace TutBackend.Repositories;

public class DriverLocationRepository(TutDbContext context) : Repository<DriverLocation>(context), IDriverLocationRepository;

