using Tut.Common.Models;
using TutBackend.Data;
namespace TutBackend.Repositories;

public class PlaceRepository(TutDbContext context) : Repository<Place>(context), IPlaceRepository;
