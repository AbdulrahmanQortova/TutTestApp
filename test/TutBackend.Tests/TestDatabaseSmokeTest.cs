using System.Linq;
using Microsoft.EntityFrameworkCore;
using Tut.Common.Models;
using Xunit;

namespace TutBackend.Tests;

[Collection("Database collection")]
public class TestDatabaseSmokeTest(TestDatabaseFixture fixture)
{

    [Fact]
    public void SeededData_HasExpectedCounts_AndStopsPerTrip()
    {
        // Use the fixture's context which was seeded in InitializeAsync
        var ctx = fixture.Context;

        var driverCount = ctx.Drivers.Count();
        var userCount = ctx.Users.Count();
        var tripCount = ctx.Trips.Include(t => t.Stops).Count();

        Assert.True(driverCount >= 3, $"Expected at least 3 drivers but found {driverCount}");
        Assert.True(userCount >= 5, $"Expected at least 5 users but found {userCount}");
        Assert.True(tripCount >= 20, $"Expected at least 20 trips but found {tripCount}");

        var trips = ctx.Trips.Include(t => t.Stops).ToList();
        Assert.All(trips, t => Assert.True(t.Stops.Count >= 2, "Each trip must have at least 2 stops"));

        // Ensure some trips have more than 2 stops
        Assert.Contains(trips, t => t.Stops.Count > 2);
    }
}

