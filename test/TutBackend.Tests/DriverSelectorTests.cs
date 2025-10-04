using Tut.Common.Models;
using TutBackend.Repositories;
using TutBackend.Services;

namespace TutBackend.Tests;

public class DriverSelectorTests
{
    private class FakeDriverRepository(IEnumerable<Driver> drivers) : IDriverRepository
    {
        private readonly List<Driver> _drivers = drivers.ToList();

        public Task<Driver?> GetByIdDetailedAsync(int id) => Task.FromResult(_drivers.SingleOrDefault(d => d.Id == id));
        public Task<Driver?> GetByMobileAsync(string mobile) => Task.FromResult(_drivers.SingleOrDefault(d => d.Mobile == mobile));
        public Task<List<Driver>> GetByIdsAsync(IEnumerable<int> ids) => Task.FromResult(_drivers.Where(d => ids.Contains(d.Id)).ToList());

        // IRepository implementations (not needed for these tests)
        public Task<IEnumerable<Driver>> GetAllAsync() => throw new NotImplementedException();
        public Task<Driver?> GetByIdAsync(int id) => Task.FromResult(_drivers.SingleOrDefault(d => d.Id == id));
        public Task<Driver> AddAsync(Driver entity) => throw new NotImplementedException();
        public Task UpdateAsync(Driver entity) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
    }

    private class FakeDriverLocationRepository(IEnumerable<DriverLocation> locations) : IDriverLocationRepository
    {
        private readonly List<DriverLocation> _locations = locations.ToList();

        public Task<List<DriverLocation>> GetLatestDriverLocations() => Task.FromResult(_locations.ToList());
        public Task<List<DriverLocation>> GetLocationHistoryForDriver(int driverId, DateTime since) => throw new NotImplementedException();

        // IRepository implementations (not needed for these tests)
        public Task<IEnumerable<DriverLocation>> GetAllAsync() => throw new NotImplementedException();
        public Task<DriverLocation?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<DriverLocation> AddAsync(DriverLocation entity) => throw new NotImplementedException();
        public Task UpdateAsync(DriverLocation entity) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
    }

    [Fact]
    public async Task FindBestDriver_HappyPath_SelectsNearestAvailable()
    {
        // Pickup location
        var pickup = new GLocation { Latitude = 30.0444, Longitude = 31.2357, Timestamp = DateTime.UtcNow };

        // Drivers
        var drivers = new List<Driver>
        {
            new Driver { Id = 1, State = DriverState.Available },
            new Driver { Id = 2, State = DriverState.Available },
            new Driver { Id = 3, State = DriverState.Offline }
        };

        // Driver locations: driver 1 is closest to pickup, driver 2 is far
        var locations = new List<DriverLocation>
        {
            new DriverLocation { DriverId = 1, Location = new GLocation { Latitude = 30.0445, Longitude = 31.2358, Timestamp = DateTime.UtcNow } },
            new DriverLocation { DriverId = 2, Location = new GLocation { Latitude = 29.9792, Longitude = 31.1342, Timestamp = DateTime.UtcNow } },
            new DriverLocation { DriverId = 3, Location = new GLocation { Latitude = 30.0459, Longitude = 31.2243, Timestamp = DateTime.UtcNow } },
        };

        var trip = new Trip
        {
            Id = 100,
            User = new User { Mobile = "x" },
            Stops = [new Stop { Place = new Place { Location = pickup } } ]
        };

        var driverRepo = new FakeDriverRepository(drivers);
        var locRepo = new FakeDriverLocationRepository(locations);

        var selector = new DriverSelector();
        var best = await selector.FindBestDriverAsync(trip, [], locRepo, driverRepo);

        Assert.NotNull(best);
        Assert.Equal(1, best.Id);
    }

    [Fact]
    public async Task FindBestDriver_NoLocations_ReturnsNull()
    {
        var pickup = new GLocation { Latitude = 30.0444, Longitude = 31.2357, Timestamp = DateTime.UtcNow };
        var trip = new Trip
        {
            Id = 101,
            User = new User { Mobile = "x" },
            Stops = [new Stop { Place = new Place { Location = pickup } } ]
        };

        var driverRepo = new FakeDriverRepository(new List<Driver>());
        var locRepo = new FakeDriverLocationRepository(new List<DriverLocation>());

        var selector = new DriverSelector();
        var best = await selector.FindBestDriverAsync(trip, [], locRepo, driverRepo);

        Assert.Null(best);
    }

    [Fact]
    public async Task FindBestDriver_NoAvailableDrivers_ReturnsNull()
    {
        var pickup = new GLocation { Latitude = 30.0444, Longitude = 31.2357, Timestamp = DateTime.UtcNow };

        var drivers = new List<Driver>
        {
            new Driver { Id = 1, State = DriverState.Offline },
            new Driver { Id = 2, State = DriverState.Inactive }
        };

        var locations = new List<DriverLocation>
        {
            new DriverLocation { DriverId = 1, Location = new GLocation { Latitude = 30.0445, Longitude = 31.2358, Timestamp = DateTime.UtcNow } },
            new DriverLocation { DriverId = 2, Location = new GLocation { Latitude = 29.9792, Longitude = 31.1342, Timestamp = DateTime.UtcNow } },
        };

        var trip = new Trip
        {
            Id = 102,
            User = new User { Mobile = "x" },
            Stops = [new Stop { Place = new Place { Location = pickup } } ]
        };

        var driverRepo = new FakeDriverRepository(drivers);
        var locRepo = new FakeDriverLocationRepository(locations);

        var selector = new DriverSelector();
        var best = await selector.FindBestDriverAsync(trip, [], locRepo, driverRepo);

        Assert.Null(best);
    }

    [Fact]
    public async Task FindBestDriver_Exclusion_Honored()
    {
        var pickup = new GLocation { Latitude = 30.0444, Longitude = 31.2357, Timestamp = DateTime.UtcNow };

        var drivers = new List<Driver>
        {
            new Driver { Id = 1, State = DriverState.Available },
            new Driver { Id = 2, State = DriverState.Available }
        };

        var locations = new List<DriverLocation>
        {
            new DriverLocation { DriverId = 1, Location = new GLocation { Latitude = 30.0445, Longitude = 31.2358, Timestamp = DateTime.UtcNow } },
            new DriverLocation { DriverId = 2, Location = new GLocation { Latitude = 29.9792, Longitude = 31.1342, Timestamp = DateTime.UtcNow } },
        };

        var trip = new Trip { Id = 200, User = new User { Mobile = "x" }, Stops =  [new Stop { Place = new Place { Location = pickup } }] };

        var driverRepo = new FakeDriverRepository(drivers);
        var locRepo = new FakeDriverLocationRepository(locations);

        var selector = new DriverSelector();
        var best = await selector.FindBestDriverAsync(trip,  [1], locRepo, driverRepo);

        Assert.NotNull(best);
        Assert.Equal(2, best.Id);
    }

    [Fact]
    public async Task FindBestDriver_RepoMissingDriver_RecordIgnored()
    {
        // driver 1 has a location but no driver record in repository
        var pickup = new GLocation { Latitude = 30.0444, Longitude = 31.2357, Timestamp = DateTime.UtcNow };

        var drivers = new List<Driver>
        {
            new Driver { Id = 2, State = DriverState.Available }
        };

        var locations = new List<DriverLocation>
        {
            new DriverLocation { DriverId = 1, Location = new GLocation { Latitude = 30.0445, Longitude = 31.2358, Timestamp = DateTime.UtcNow } },
            new DriverLocation { DriverId = 2, Location = new GLocation { Latitude = 29.9792, Longitude = 31.1342, Timestamp = DateTime.UtcNow } },
        };

        var trip = new Trip { Id = 201, User = new User { Mobile = "x" }, Stops = [new Stop { Place = new Place { Location = pickup } }] };

        var driverRepo = new FakeDriverRepository(drivers);
        var locRepo = new FakeDriverLocationRepository(locations);

        var selector = new DriverSelector();
        var best = await selector.FindBestDriverAsync(trip, [], locRepo, driverRepo);

        Assert.NotNull(best);
        Assert.Equal(2, best.Id);
    }

    [Fact]
    public async Task FindBestDriver_CostFunctionThrows_SkipsDriver()
    {
        var pickup = new GLocation { Latitude = 0, Longitude = 0, Timestamp = DateTime.UtcNow };

        var drivers = new List<Driver>
        {
            new Driver { Id = 1, State = DriverState.Available },
            new Driver { Id = 2, State = DriverState.Available }
        };

        // driver 2 location marked with special latitude to trigger throw
        var locations = new List<DriverLocation>
        {
            new DriverLocation { DriverId = 2, Location = new GLocation { Latitude = 99, Longitude = 0, Timestamp = DateTime.UtcNow } },
            new DriverLocation { DriverId = 1, Location = new GLocation { Latitude = 1, Longitude = 0, Timestamp = DateTime.UtcNow } },
        };

        var trip = new Trip { Id = 202, User = new User { Mobile = "x" }, Stops = [ new Stop { Place = new Place { Location = pickup } } ] };

        var driverRepo = new FakeDriverRepository(drivers);
        var locRepo = new FakeDriverLocationRepository(locations);

        // cost function that throws for locations with Latitude == 99
        double CostWithThrow(GLocation p, GLocation d)
        {
            if (Math.Abs(d.Latitude - 99) < 0.000001) throw new InvalidOperationException("bad location");
            var dx = p.Latitude - d.Latitude;
            var dy = p.Longitude - d.Longitude;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        var selector = new DriverSelector(null, CostWithThrow);
        var best = await selector.FindBestDriverAsync(trip, [], locRepo, driverRepo);

        Assert.NotNull(best);
        Assert.Equal(1, best.Id);
    }

    [Fact]
    public async Task FindBestDriver_TieBreaking_FirstOccurrenceWins()
    {
        // pickup at origin
        var pickup = new GLocation { Latitude = 0, Longitude = 0, Timestamp = DateTime.UtcNow };

        var drivers = new List<Driver>
        {
            new Driver { Id = 1, State = DriverState.Available },
            new Driver { Id = 2, State = DriverState.Available }
        };

        // both drivers at distance 1 from pickup; driver 1 appears earlier in locations
        var locations = new List<DriverLocation>
        {
            new DriverLocation { DriverId = 1, Location = new GLocation { Latitude = 1, Longitude = 0, Timestamp = DateTime.UtcNow } },
            new DriverLocation { DriverId = 2, Location = new GLocation { Latitude = -1, Longitude = 0, Timestamp = DateTime.UtcNow } },
        };

        var trip = new Trip { Id = 203, User = new User { Mobile = "x" }, Stops = [new Stop { Place = new Place { Location = pickup } } ] };

        var driverRepo = new FakeDriverRepository(drivers);
        var locRepo = new FakeDriverLocationRepository(locations);

        var selector = new DriverSelector();
        var best = await selector.FindBestDriverAsync(trip, [], locRepo, driverRepo);

        Assert.NotNull(best);
        Assert.Equal(1, best.Id);
    }
}
