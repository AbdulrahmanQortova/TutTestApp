using Xunit;
using Microsoft.EntityFrameworkCore;
using TutBackend.Data;
using TutBackend.Repositories;
using Tut.Common.Models;
using TutBackend.Services;

namespace TutBackend.Tests;

public class DriverLocationRepositoryTests
{
    private TutDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TutDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TutDbContext(options);
    }

    [Fact]
    public async Task GetLatestDriverLocations_ReturnsLatestLocationPerDriver()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var repository = new DriverLocationRepository(context);

        var oldLocation1 = new DriverLocation
        {
            DriverId = 1,
            DriverName = "Driver1",
            DriverState = DriverState.Available,
            Latitude = 30.0,
            Longitude = 31.0,
            Timestamp = DateTime.UtcNow.AddMinutes(-10)
        };

        var newLocation1 = new DriverLocation
        {
            DriverId = 1,
            DriverName = "Driver1",
            DriverState = DriverState.Available,
            Latitude = 30.1,
            Longitude = 31.1,
            Timestamp = DateTime.UtcNow
        };

        var location2 = new DriverLocation
        {
            DriverId = 2,
            DriverName = "Driver2",
            DriverState = DriverState.Available,
            Latitude = 30.2,
            Longitude = 31.2,
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        };

        await repository.SaveDriverLocationAsync(oldLocation1);
        await repository.SaveDriverLocationAsync(newLocation1);
        await repository.SaveDriverLocationAsync(location2);

        // Act
        var result = await repository.GetLatestDriverLocations();

        // Assert
        Assert.Equal(2, result.Count);
        var driver1Location = result.FirstOrDefault(l => l.DriverId == 1);
        Assert.NotNull(driver1Location);
        Assert.Equal(30.1, driver1Location.Latitude);
        Assert.Equal(31.1, driver1Location.Longitude);
    }

    [Fact]
    public async Task GetLatestDriverLocations_WithNoLocations_ReturnsEmpty()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var repository = new DriverLocationRepository(context);

        // Act
        var result = await repository.GetLatestDriverLocations();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLocationHistoryForDriver_ReturnsLocationsForSpecificDriver()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var repository = new DriverLocationRepository(context);
        var since = DateTime.UtcNow.AddHours(-1);

        var location1 = new DriverLocation
        {
            DriverId = 1,
            DriverName = "Driver1",
            DriverState = DriverState.Available,
            Latitude = 30.0,
            Longitude = 31.0,
            Timestamp = DateTime.UtcNow.AddMinutes(-30)
        };

        var location2 = new DriverLocation
        {
            DriverId = 1,
            DriverName = "Driver1",
            DriverState = DriverState.Available,
            Latitude = 30.1,
            Longitude = 31.1,
            Timestamp = DateTime.UtcNow.AddMinutes(-20)
        };

        var location3 = new DriverLocation
        {
            DriverId = 2,
            DriverName = "Driver2",
            DriverState = DriverState.Available,
            Latitude = 30.2,
            Longitude = 31.2,
            Timestamp = DateTime.UtcNow.AddMinutes(-15)
        };

        await repository.AddAsync(location1);
        await repository.AddAsync(location2);
        await repository.AddAsync(location3);

        // Act
        var result = await repository.GetLocationHistoryForDriver(1, since);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, l => Assert.Equal(1, l.DriverId));
    }

    [Fact]
    public async Task GetLocationHistoryForDriver_WithOldDate_ReturnsEmpty()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var repository = new DriverLocationRepository(context);
        var since = DateTime.UtcNow.AddHours(-2);

        var location = new DriverLocation
        {
            DriverId = 1,
            DriverName = "Driver1",
            DriverState = DriverState.Available,
            Latitude = 30.0,
            Longitude = 31.0,
            Timestamp = DateTime.UtcNow.AddHours(-3)
        };

        await repository.AddAsync(location);

        // Act
        var result = await repository.GetLocationHistoryForDriver(1, since);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLocationHistoryForDriver_OrdersByTimestampDescending()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var repository = new DriverLocationRepository(context);
        var since = DateTime.UtcNow.AddHours(-1);

        var location1 = new DriverLocation
        {
            DriverId = 1,
            DriverName = "Driver1",
            DriverState = DriverState.Available,
            Latitude = 30.0,
            Longitude = 31.0,
            Timestamp = DateTime.UtcNow.AddMinutes(-30)
        };

        var location2 = new DriverLocation
        {
            DriverId = 1,
            DriverName = "Driver1",
            DriverState = DriverState.Available,
            Latitude = 30.1,
            Longitude = 31.1,
            Timestamp = DateTime.UtcNow.AddMinutes(-10)
        };

        await repository.AddAsync(location1);
        await repository.AddAsync(location2);

        // Act
        var result = await repository.GetLocationHistoryForDriver(1, since);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result[0].Timestamp >= result[1].Timestamp);
    }
}
