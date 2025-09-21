using TutBackend.Data;
using Tut.Common.Models;

namespace TutBackend.Repositories;

public class LocationRepository(TutDbContext context) : Repository<GLocation>(context), ILocationRepository;

