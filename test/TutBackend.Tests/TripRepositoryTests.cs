using Microsoft.EntityFrameworkCore;
using TutBackend.Data;
using TutBackend.Repositories;
using Tut.Common.Models;

namespace TutBackend.Tests;

public class TripRepositoryTests
{
    private static TutDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TutDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TutDbContext(options);
    }

    [Fact]
    public async Task GetOneUnassignedTripAsync_WithUnassignedTrips_ReturnsOldestTrip()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new TripRepository(context);
        var user = new User { Mobile = "1234567890", FirstName = "Test", LastName = "User" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var trip1 = new Trip { User = user, Status = TripState.Requested, CreatedAt = DateTime.UtcNow.AddMinutes(-5), Stops = [] };
        var trip2 = new Trip { User = user, Status = TripState.Requested, CreatedAt = DateTime.UtcNow.AddMinutes(-10), Stops = [] };
        var trip3 = new Trip { User = user, Status = TripState.Requested, CreatedAt = DateTime.UtcNow.AddMinutes(-2), Stops = [] };
        context.Trips.AddRange(trip1, trip2, trip3);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetOneUnassignedTripAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(trip2.Id, result.Id);
    }

    // ...existing code...

    [Fact]
    public async Task GetOneUnassignedTripAsync_WithAssignedTrips_ReturnsNull()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new TripRepository(context);
        var user = new User { Mobile = "1234567890", FirstName = "Test", LastName = "User" };
        var driver = new Driver { Mobile = "0987654321", FirstName = "Test", LastName = "Driver" };
        context.Users.Add(user);
        context.Drivers.Add(driver);
        await context.SaveChangesAsync();

        var trip = new Trip { User = user, Driver = driver, Status = TripState.Requested, Stops = [] };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetOneUnassignedTripAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveTripForUser_WithActiveTrip_ReturnsTrip()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new TripRepository(context);
        var user = new User { Mobile = "1234567890", FirstName = "Test", LastName = "User" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var activeTrip = new Trip { User = user, Status = TripState.Requested, Stops = [] };
        context.Trips.Add(activeTrip);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetActiveTripForUser(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(activeTrip.Id, result.Id);
    }

    [Fact]
    public async Task GetActiveTripForUser_WithEndedTrip_ReturnsNull()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new TripRepository(context);
        var user = new User { Mobile = "1234567890", FirstName = "Test", LastName = "User" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var endedTrip = new Trip { User = user, Status = TripState.Ended, Stops = [] };
        context.Trips.Add(endedTrip);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetActiveTripForUser(user.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveTripForDriver_WithActiveTrip_ReturnsTrip()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new TripRepository(context);
        var driver = new Driver { Mobile = "0987654321", FirstName = "Test", LastName = "Driver" };
        var user = new User { Mobile = "1234567890", FirstName = "Test", LastName = "User" };
        context.Drivers.Add(driver);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var activeTrip = new Trip { User = user, Driver = driver, Status = TripState.Requested, Stops = [] };
        context.Trips.Add(activeTrip);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetActiveTripForDriver(driver.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(activeTrip.Id, result.Id);
    }

    [Fact]
    public async Task GetTripsForUser_ReturnsUserTrips()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new TripRepository(context);
        var user1 = new User { Mobile = "1111111111", FirstName = "User1", LastName = "Test" };
        var user2 = new User { Mobile = "2222222222", FirstName = "User2", LastName = "Test" };
        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        await repository.AddAsync(new Trip { User = user1, Status = TripState.Requested, Stops = [] });
        await repository.AddAsync(new Trip { User = user1, Status = TripState.Ended, Stops = [] });
        await repository.AddAsync(new Trip { User = user2, Status = TripState.Requested, Stops = [] });

        // Act
        var result = await repository.GetTripsForUser(user1.Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, trip => Assert.NotNull(trip.User));
        Assert.All(result, trip => Assert.Equal(user1.Id, trip.User!.Id));
    }

    [Fact]
    public async Task GetTripsForDriver_ReturnsDriverTrips()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new TripRepository(context);
        var driver1 = new Driver { Mobile = "1111111111", FirstName = "Driver1", LastName = "Test" };
        var driver2 = new Driver { Mobile = "2222222222", FirstName = "Driver2", LastName = "Test" };
        var user = new User { Mobile = "3333333333", FirstName = "User", LastName = "Test" };
        context.Drivers.AddRange(driver1, driver2);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        await repository.AddAsync(new Trip { User = user, Driver = driver1, Status = TripState.Requested, Stops = [] });
        await repository.AddAsync(new Trip { User = user, Driver = driver1, Status = TripState.Ended, Stops = [] });
        await repository.AddAsync(new Trip { User = user, Driver = driver2, Status = TripState.Requested, Stops = [] });

        // Act
        var result = await repository.GetTripsForDriver(driver1.Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, trip => Assert.NotNull(trip.Driver));
        Assert.All(result, trip => Assert.Equal(driver1.Id, trip.Driver!.Id));
    }

    [Fact]
    public async Task GetActiveTripsAsync_ReturnsOnlyActiveTrips()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new TripRepository(context);
        var user = new User { Mobile = "1234567890", FirstName = "Test", LastName = "User" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        await repository.AddAsync(new Trip { User = user, Status = TripState.Requested, Stops = [] });
        await repository.AddAsync(new Trip { User = user, Status = TripState.Requested, Stops = [] });
        await repository.AddAsync(new Trip { User = user, Status = TripState.Ended, Stops = [] });
        await repository.AddAsync(new Trip { User = user, Status = TripState.Canceled, Stops = [] });

        // Act
        var result = await repository.GetActiveTripsAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, trip => Assert.Contains(trip.Status, new[] { TripState.Requested, TripState.Ongoing }));
    }

    [Fact]
    public async Task GetAllTripsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new TripRepository(context);
        var user = new User { Mobile = "1234567890", FirstName = "Test", LastName = "User" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        for (int i = 0; i < 10; i++)
        {
            await repository.AddAsync(new Trip { User = user, Status = TripState.Requested, Stops = [] });
        }
        await repository.AddAsync(new Trip { User = user, Status = TripState.Ended, Stops = [] });
        await repository.AddAsync(new Trip { User = user, Status = TripState.Canceled, Stops = [] });

        // Act
        var result = await repository.GetAllTripsAsync(5, 1);

        // Assert
        Assert.Equal(5, result.Count());
        Assert.All(result, trip => Assert.NotNull(trip.User));
        Assert.All(result, trip => Assert.Equal(user.Id, trip.User!.Id));
    }
}
