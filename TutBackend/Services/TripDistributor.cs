using Tut.Common.Models;
using Tut.Common.Utils;
using TutBackend.Repositories;
namespace TutBackend.Services;

public class TripDistributor(
        ITripRepository tripRepository,
        IDriverLocationRepository driverLocationRepository,
        IDriverRepository driverRepository
    )
{
    private async Task<Driver?> FindBestDriver(int[] excludedIds)
    {
    }

    private Func<GLocation, GLocation, double> _costFunction = CartesianDistance;


    private static readonly Func<GLocation, GLocation, double> CartesianDistance = LocationUtils.DistanceInMeters;
}
