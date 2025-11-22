using Xunit;
using Microsoft.EntityFrameworkCore;
using TutBackend.Data;
using TutBackend.Repositories;
using Tut.Common.Models;
using TutBackend.Services;

namespace TutBackend.Tests;

public class DriverRepositoryTests
{
    private TutDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TutDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TutDbContext(options);
    }

    [Fact]
    public async Task GetByMobileAsync_WithExistingDriver_ReturnsDriver()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var repository = new DriverRepository(context);
        var driver = new Driver { Mobile = "1234567890", FirstName = "John", LastName = "Driver" };
        await repository.AddAsync(driver);

        // Act
        var result = await repository.GetByMobileAsync("1234567890");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("1234567890", result.Mobile);
    }

    [Fact]
    public async Task GetByMobileAsync_WithNonExistingDriver_ReturnsNull()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var repository = new DriverRepository(context);

        // Act
        var result = await repository.GetByMobileAsync("9999999999");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdsAsync_WithValidIds_ReturnsDrivers()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var repository = new DriverRepository(context);
        var driver1 = await repository.AddAsync(new Driver { Mobile = "1111111111", FirstName = "Driver1", LastName = "Test" });
        var driver2 = await repository.AddAsync(new Driver { Mobile = "2222222222", FirstName = "Driver2", LastName = "Test" });
        await repository.AddAsync(new Driver { Mobile = "3333333333", FirstName = "Driver3", LastName = "Test" });

        // Act
        var result = await repository.GetByIdsAsync([driver1.Id, driver2.Id]);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Mobile == "1111111111");
        Assert.Contains(result, d => d.Mobile == "2222222222");
    }

    [Fact]
    public async Task GetByIdsAsync_WithEmptyList_ReturnsEmpty()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var repository = new DriverRepository(context);

        // Act
        var result = await repository.GetByIdsAsync([]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdsAsync_WithInvalidIds_ReturnsEmpty()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var repository = new DriverRepository(context);

        // Act
        var result = await repository.GetByIdsAsync([0, -1, -5]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdsAsync_WithDuplicateIds_ReturnsDistinct()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var repository = new DriverRepository(context);
        var driver = await repository.AddAsync(new Driver { Mobile = "1111111111", FirstName = "Driver1", LastName = "Test" });

        // Act
        var result = await repository.GetByIdsAsync([driver.Id, driver.Id, driver.Id]);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetAllDriversAsync_ReturnsDriversWithStats()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var repository = new DriverRepository(context);
        var driver = await repository.AddAsync(new Driver { Mobile = "1111111111", FirstName = "Driver1", LastName = "Test" });

        // Act
        var result = await repository.GetAllDriversAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(0, result[0].TotalTrips);
        Assert.Equal(0, result[0].TotalEarnings);
    }

    [Fact]
    public async Task GetByIdDetailedAsync_WithExistingDriver_ReturnsDriverWithRelations()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var repository = new DriverRepository(context);
        var driver = await repository.AddAsync(new Driver { Mobile = "1111111111", FirstName = "Driver1", LastName = "Test" });

        // Act
        var result = await repository.GetByIdDetailedAsync(driver.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(driver.Id, result.Id);
        Assert.NotNull(result.Trips);
    }

    [Fact]
    public async Task GetByIdDetailedAsync_WithNonExistingDriver_ReturnsNull()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var repository = new DriverRepository(context);

        // Act
        var result = await repository.GetByIdDetailedAsync(99999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_AddsDriverToDatabase()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var repository = new DriverRepository(context);
        var driver = new Driver { Mobile = "5555555555", FirstName = "Jane", LastName = "Driver", State = DriverState.Available };

        // Act
        var addedDriver = await repository.AddAsync(driver);

        // Assert
        Assert.NotNull(addedDriver);
        Assert.True(addedDriver.Id > 0);
        Assert.Equal(DriverState.Available, addedDriver.State);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesDriverState()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var repository = new DriverRepository(context);
        var driver = new Driver { Mobile = "1111111111", FirstName = "Test", LastName = "Driver", State = DriverState.Available };
        await repository.AddAsync(driver);

        // Act
        driver.State = DriverState.OnTrip;
        await repository.UpdateAsync(driver);

        // Assert
        var updated = await repository.GetByIdAsync(driver.Id);
        Assert.NotNull(updated);
        Assert.Equal(DriverState.OnTrip, updated.State);
    }
}
