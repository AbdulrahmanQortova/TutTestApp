using TutBackend.Data;
using Tut.Common.Models;

namespace TutBackend.Repositories;

public class SavedPlaceRepository(TutDbContext context) : Repository<SavedPlace>(context), ISavedPlaceRepository;

