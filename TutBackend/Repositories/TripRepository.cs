using TutBackend.Data;
using Tut.Common.Models;

namespace TutBackend.Repositories;

public class TripRepository(TutDbContext context) : Repository<Trip>(context), ITripRepository;

