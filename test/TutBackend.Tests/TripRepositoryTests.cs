using Microsoft.EntityFrameworkCore;
using TutBackend.Data;
using TutBackend.Repositories;
using Tut.Common.Models;

namespace TutBackend.Tests;

public class TripRepositoryTests
{
    private static TutDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TutDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new TutDbContext(options);
    }

    [Fact]
    public async Task GetAllTripsAsync_ReturnsDescendingOrderedAndLimited()
    {
        // Arrange
        var dbName = nameof(GetAllTripsAsync_ReturnsDescendingOrderedAndLimited) + Guid.NewGuid();
        await using var context = CreateContext(dbName);

        var user = new User { Mobile = "100", FirstName = "U1" };
        var driver = new Driver { Mobile = "200", FirstName = "D1" };
        await context.AddRangeAsync(user, driver);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var trips = new List<Trip>
        {
            new Trip { User = user, Driver = driver, CreatedAt = now.AddMinutes(-1), Status = TripState.Requested },
            new Trip { User = user, Driver = driver, CreatedAt = now.AddMinutes(-2), Status = TripState.Requested },
            new Trip { User = user, Driver = driver, CreatedAt = now.AddMinutes(-3), Status = TripState.Requested },
        };
        await context.AddRangeAsync(trips);
        await context.SaveChangesAsync();

        var repo = new TripRepository(context);

        // Act
        var result = await repo.GetAllTripsAsync(take: 2, skip: 0);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result[0].CreatedAt >= result[1].CreatedAt, "Expected descending CreatedAt order");
    }

    [Fact]
    public async Task GetActiveTripsAsync_ReturnsOnlyActiveStatuses_OrderedAscending()
    {
        // Arrange
        var dbName = nameof(GetActiveTripsAsync_ReturnsOnlyActiveStatuses_OrderedAscending) + Guid.NewGuid();
        await using var context = CreateContext(dbName);

        var user = new User { Mobile = "u1" };
        var driver = new Driver { Mobile = "d1" };
        await context.AddRangeAsync(user, driver);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var active1 = new Trip { User = user, Driver = driver, CreatedAt = now.AddMinutes(-10), Status = TripState.Requested };
        var active2 = new Trip { User = user, Driver = driver, CreatedAt = now.AddMinutes(-5), Status = TripState.Started };
        var inactive = new Trip { User = user, Driver = driver, CreatedAt = now.AddMinutes(-1), Status = TripState.Ended };

        await context.AddRangeAsync(active1, active2, inactive);
        await context.SaveChangesAsync();

        var repo = new TripRepository(context);

        // Act
        var result = await repo.GetActiveTripsAsync(take: 10, skip: 0);

        // Assert
        Assert.All(result, t => Assert.True(t.Status != TripState.Unspecified && t.Status != TripState.Ended && t.Status != TripState.Canceled));
        // Ordered ascending by CreatedAt
        for (int i = 1; i < result.Count; i++)
        {
            Assert.True(result[i - 1].CreatedAt <= result[i].CreatedAt, "Expected ascending CreatedAt order for active trips");
        }
    }

    [Fact]
    public async Task GetTripsForUser_ReturnsOnlyThatUsersTrips_WithPaging()
    {
        // Arrange
        var dbName = nameof(GetTripsForUser_ReturnsOnlyThatUsersTrips_WithPaging) + Guid.NewGuid();
        await using var context = CreateContext(dbName);

        var user1 = new User { Mobile = "u1" };
        var user2 = new User { Mobile = "u2" };
        var driver = new Driver { Mobile = "d1" };
        await context.AddRangeAsync(user1, user2, driver);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var trips = new List<Trip>
        {
            new Trip { User = user1, Driver = driver, CreatedAt = now.AddMinutes(-3), Status = TripState.Requested },
            new Trip { User = user1, Driver = driver, CreatedAt = now.AddMinutes(-2), Status = TripState.Requested },
            new Trip { User = user2, Driver = driver, CreatedAt = now.AddMinutes(-1), Status = TripState.Requested },
        };
        await context.AddRangeAsync(trips);
        await context.SaveChangesAsync();

        var repo = new TripRepository(context);

        // Act
        var result = await repo.GetTripsForUser(user1.Id, take: 10, skip: 0);

        // Assert
        Assert.All(result, t => Assert.Equal(user1.Id, t.User.Id));
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTripsForDriver_ReturnsOnlyThatDriversTrips_WithPaging()
    {
        // Arrange
        var dbName = nameof(GetTripsForDriver_ReturnsOnlyThatDriversTrips_WithPaging) + Guid.NewGuid();
        await using var context = CreateContext(dbName);

        var user = new User { Mobile = "u1" };
        var driver1 = new Driver { Mobile = "d1" };
        var driver2 = new Driver { Mobile = "d2" };
        await context.AddRangeAsync(user, driver1, driver2);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var trips = new List<Trip>
        {
            new Trip { User = user, Driver = driver1, CreatedAt = now.AddMinutes(-3), Status = TripState.Requested },
            new Trip { User = user, Driver = driver1, CreatedAt = now.AddMinutes(-2), Status = TripState.Requested },
            new Trip { User = user, Driver = driver2, CreatedAt = now.AddMinutes(-1), Status = TripState.Requested },
        };
        await context.AddRangeAsync(trips);
        await context.SaveChangesAsync();

        var repo = new TripRepository(context);

        // Act
        var result = await repo.GetTripsForDriver(driver1.Id, take: 10, skip: 0);

        // Assert
        Assert.All(result, t => Assert.NotNull(t.Driver));
        Assert.All(result, t => Assert.Equal(driver1.Id, t.Driver!.Id));
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetActiveTripForUser_ReturnsSingleActiveTrip_WhenOneExists()
    {
        // Arrange
        var dbName = nameof(GetActiveTripForUser_ReturnsSingleActiveTrip_WhenOneExists) + Guid.NewGuid();
        await using var context = CreateContext(dbName);

        var user = new User { Mobile = "u1" };
        var driver = new Driver { Mobile = "d1" };
        await context.AddRangeAsync(user, driver);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        // one active, one ended
        var active = new Trip { User = user, Driver = driver, CreatedAt = now.AddMinutes(-5), Status = TripState.DriverArrived };
        var ended = new Trip { User = user, Driver = driver, CreatedAt = now.AddMinutes(-1), Status = TripState.Ended };

        await context.AddRangeAsync(active, ended);
        await context.SaveChangesAsync();

        var repo = new TripRepository(context);

        // Act
        var got = await repo.GetActiveTripForUser(user.Id);

        // Assert
        Assert.NotNull(got);
        Assert.Equal(user.Id, got.User.Id);
        Assert.True(got.Status != TripState.Unspecified && got.Status != TripState.Ended && got.Status != TripState.Canceled);
    }

    [Fact]
    public async Task GetActiveTripForDriver_ReturnsSingleActiveTrip_WhenOneExists()
    {
        // Arrange
        var dbName = nameof(GetActiveTripForDriver_ReturnsSingleActiveTrip_WhenOneExists) + Guid.NewGuid();
        await using var context = CreateContext(dbName);

        var user = new User { Mobile = "u1" };
        var driver = new Driver { Mobile = "d1" };
        await context.AddRangeAsync(user, driver);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        // one active, one ended
        var active = new Trip { User = user, Driver = driver, CreatedAt = now.AddMinutes(-5), Status = TripState.Started };
        var ended = new Trip { User = user, Driver = driver, CreatedAt = now.AddMinutes(-1), Status = TripState.Ended };

        await context.AddRangeAsync(active, ended);
        await context.SaveChangesAsync();

        var repo = new TripRepository(context);

        // Act
        var got = await repo.GetActiveTripForDriver(driver.Id);

        // Assert
        Assert.NotNull(got);
        Assert.NotNull(got.Driver);
        Assert.Equal(driver.Id, got.Driver.Id);
        Assert.True(got.Status != TripState.Unspecified && got.Status != TripState.Ended && got.Status != TripState.Canceled);
    }

    [Fact]
    public async Task GetOneUnassignedTripAsync_ReturnsEarliestUnassignedTrip_WithIncludes()
    {
        // Arrange
        var dbName = nameof(GetOneUnassignedTripAsync_ReturnsEarliestUnassignedTrip_WithIncludes) + Guid.NewGuid();
        await using var context = CreateContext(dbName);

        var user = new User { Mobile = "u1" };
        var driver = new Driver { Mobile = "d1" };
        var place = new Place { Name = "P1", Location = new GLocation { Latitude = 1, Longitude = 1 } };
        await context.AddRangeAsync(user, driver, place);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var earlier = new Trip
        {
            User = user,
            Driver = null,
            RequestedDriverPlace = place,
            RequestingPlace = place,
            CreatedAt = now.AddMinutes(-10),
            Status = TripState.Requested,
            Stops = new List<Stop> { new Stop { Place = place } }
        };

        var later = new Trip
        {
            User = user,
            Driver = null,
            RequestedDriverPlace = place,
            RequestingPlace = place,
            CreatedAt = now.AddMinutes(-1),
            Status = TripState.Requested,
            Stops = new List<Stop> { new Stop { Place = place } }
        };

        // also add a trip that has a driver assigned which should be ignored
        var assigned = new Trip
        {
            User = user,
            Driver = driver,
            RequestedDriverPlace = place,
            RequestingPlace = place,
            CreatedAt = now.AddMinutes(-20),
            Status = TripState.Requested,
            Stops = new List<Stop> { new Stop { Place = place } }
        };

        await context.AddRangeAsync(earlier, later, assigned);
        await context.SaveChangesAsync();

        var repo = new TripRepository(context);

        // Act
        var got = await repo.GetOneUnassignedTripAsync();

        // Assert
        Assert.NotNull(got);
        Assert.Null(got.Driver);
        Assert.Equal(earlier.CreatedAt, got.CreatedAt);
        Assert.NotNull(got.User);
        Assert.NotNull(got.RequestedDriverPlace);
        Assert.NotNull(got.RequestingPlace);
        Assert.NotEmpty(got.Stops);
        Assert.NotNull(got.Stops[0].Place);
    }

    [Fact]
    public async Task GetOneUnassignedTripAsync_ReturnsNull_WhenNoneAvailable()
    {
        // Arrange
        var dbName = nameof(GetOneUnassignedTripAsync_ReturnsNull_WhenNoneAvailable) + Guid.NewGuid();
        await using var context = CreateContext(dbName);

        var user = new User { Mobile = "u1" };
        var driver = new Driver { Mobile = "d1" };
        var place = new Place { Name = "P1", Location = new GLocation { Latitude = 1, Longitude = 1 } };
        await context.AddRangeAsync(user, driver, place);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var t1 = new Trip { User = user, Driver = driver, CreatedAt = now.AddMinutes(-5), Status = TripState.Requested };
        var t2 = new Trip { User = user, Driver = driver, CreatedAt = now.AddMinutes(-1), Status = TripState.Requested };
        await context.AddRangeAsync(t1, t2);
        await context.SaveChangesAsync();

        var repo = new TripRepository(context);

        // Act
        var got = await repo.GetOneUnassignedTripAsync();

        // Assert
        Assert.Null(got);
    }
}
