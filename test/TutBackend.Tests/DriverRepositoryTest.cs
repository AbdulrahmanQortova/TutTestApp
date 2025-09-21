using Microsoft.EntityFrameworkCore;
using TutBackend.Data;
using TutBackend.Repositories;
using Tut.Common.Models;

namespace TutBackend.Tests;

public class DriverRepositoryTest
{
    private static TutDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TutDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new TutDbContext(options);
    }

    [Fact]
    public async Task GetByMobileAsync_ReturnsDriver_WhenExists()
    {
        // Arrange
        var context = CreateInMemoryContext("GetByMobileDb");
        var driver = new Driver { Mobile = "+123456789", FirstName = "John", LastName = "Doe" };
        await context.Drivers.AddAsync(driver);
        await context.SaveChangesAsync();

        var repo = new DriverRepository(context);

        // Act
        var found = await repo.GetByMobileAsync("+123456789");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("John", found!.FirstName);
    }

    [Fact]
    public async Task GetByIdDetailedAsync_IncludesRelatedEntities()
    {
        // Arrange
        var context = CreateInMemoryContext("GetByIdDetailedDb");

        var user = new User { Mobile = "+999", FirstName = "Alice" };
        var place = new Place { Name = "Home" };

        var trip = new Trip
        {
            User = user,
            RequestedDriverPlace = place,
            RequestingPlace = place,
            Stops = new List<Stop> { new Stop { Place = place } },
        };

        var driver = new Driver
        {
            Mobile = "+222",
            FirstName = "DriverFirst",
            Trips = new List<Trip> { trip }
        };

        await context.Drivers.AddAsync(driver);
        await context.SaveChangesAsync();

        var repo = new DriverRepository(context);

        // Act
        var detailed = await repo.GetByIdDetailedAsync(driver.Id);

        // Assert
        Assert.NotNull(detailed);
        Assert.NotNull(detailed!.Trips);
        Assert.Single(detailed.Trips!);
        Assert.NotNull(detailed.Trips![0].User);
        Assert.Equal("Alice", detailed.Trips![0].User.FirstName);
        Assert.NotNull(detailed.Trips![0].RequestedDriverPlace);
        Assert.Equal("Home", detailed.Trips![0].RequestedDriverPlace!.Name);
        Assert.NotNull(detailed.Trips![0].Stops);
        Assert.Single(detailed.Trips![0].Stops!);
        Assert.NotNull(detailed.Trips![0].Stops![0].Place);
        Assert.Equal("Home", detailed.Trips![0].Stops![0].Place!.Name);
    }
}
