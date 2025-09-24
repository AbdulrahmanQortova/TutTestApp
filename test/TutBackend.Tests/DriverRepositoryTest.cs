using TutBackend.Repositories;

namespace TutBackend.Tests;

[Collection("Database collection")]
public class DriverRepositoryTest(TestDatabaseFixture fixture)
{

    [Fact]
    public async Task GetByMobileAsync_ReturnsDriver_WhenExists()
    {
        // Arrange
        var context = fixture.Context;
        var repo = new DriverRepository(context);

        // Act
        var found = await repo.GetByMobileAsync("810123451");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Harry", found.FirstName);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllDrivers()
    {
        // Arrange
        var context = fixture.Context;
        var repo = new DriverRepository(context);

        // Act
        var all = (await repo.GetAllAsync()).ToList();

        // Assert
        // The fixture seeds 3 drivers explicitly.
        Assert.True(all.Count >= 3, "Expected at least 3 seeded drivers");
        // Verify one known driver exists
        var harry = all.SingleOrDefault(d => d.Mobile == "810123451");
        Assert.NotNull(harry);
        Assert.Equal("Harry", harry.FirstName);
    }

    [Fact]
    public async Task UpdateAsync_ChangesDriverAndPersists()
    {
        // Arrange
        var context = fixture.Context;
        var repo = new DriverRepository(context);

        var driver = await repo.GetByMobileAsync("810123453");
        Assert.NotNull(driver);

        
        // Act
        var originalFirstName = driver.FirstName;
        driver.FirstName = originalFirstName + "_Updated";
        await repo.UpdateAsync(driver);

        // Detach tracked entity to simulate a fresh context read
        context.Entry(driver).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        var reloaded = await repo.GetByIdAsync(driver.Id);

        // Assert
        Assert.NotNull(reloaded);
        Assert.Equal(originalFirstName + "_Updated", reloaded.FirstName);
    }

    [Fact]
    public async Task GetByIdDetailedAsync_IncludesRelatedEntities()
    {
        // Arrange
        var context = fixture.Context;

        var repo = new DriverRepository(context);

        // Act
        var detailed = await repo.GetByIdDetailedAsync(1);

        // Assert
        Assert.NotNull(detailed);
        var trips = detailed.Trips;
        Assert.NotNull(trips);
        var firstTrip = trips[0];
        Assert.NotNull(firstTrip);
        Assert.NotNull(firstTrip.User);
        Assert.Equal("Donald", firstTrip.User.FirstName);
        Assert.NotNull(firstTrip.RequestedDriverPlace);
        Assert.Equal("Zamalek", firstTrip.RequestedDriverPlace.Name);
        Assert.NotNull(firstTrip.Stops);
        Assert.Equal(3, firstTrip.Stops.Count);
        Assert.NotNull(firstTrip.Stops[0].Place);
        var place0 = firstTrip.Stops[0].Place;
        Assert.NotNull(place0);
        Assert.Equal("Tahrir Square", place0.Name);
    }
}
