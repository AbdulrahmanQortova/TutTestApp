using Tut.Common.Models;
using Xunit;

namespace Tut.Common.Tests;

public class PlaceAndGLocationTests
{
    [Fact]
    public void Place_ToLocation_ConvertsCorrectly()
    {
        var place = new Place
        {
            Latitude = 40.7128,
            Longitude = -74.0060,
            PlaceType = PlaceType.Stop,
            Order = 0
        };

        var location = place.ToLocation();

        Assert.Equal(place.Latitude, location.Latitude);
        Assert.Equal(place.Longitude, location.Longitude);
    }

    [Fact]
    public void Place_WithAllProperties_CreatesSuccessfully()
    {
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };
        var place = new Place
        {
            Id = 1,
            Name = "Home",
            Address = "123 Main St",
            Latitude = 40.7128,
            Longitude = -74.0060,
            PlaceType = PlaceType.Saved,
            User = user,
            Order = 1
        };

        Assert.Equal(1, place.Id);
        Assert.Equal("Home", place.Name);
        Assert.Equal("123 Main St", place.Address);
        Assert.Equal(40.7128, place.Latitude);
        Assert.Equal(-74.0060, place.Longitude);
        Assert.Equal(PlaceType.Saved, place.PlaceType);
        Assert.Equal(user, place.User);
        Assert.Equal(1, place.Order);
    }

    [Fact]
    public void Place_DefaultStringProperties_AreEmpty()
    {
        var place = new Place
        {
            Latitude = 40.7128,
            Longitude = -74.0060,
            PlaceType = PlaceType.Stop,
            Order = 0
        };

        Assert.Equal(string.Empty, place.Name);
        Assert.Equal(string.Empty, place.Address);
    }

    [Fact]
    public void GLocation_WithAllProperties_CreatesSuccessfully()
    {
        var timestamp = DateTime.UtcNow;
        var location = new GLocation
        {
            Id = 1,
            Latitude = 40.7128,
            Longitude = -74.0060,
            Altitude = 10.5,
            Course = 45.0,
            Speed = 25.0,
            Timestamp = timestamp
        };

        Assert.Equal(1, location.Id);
        Assert.Equal(40.7128, location.Latitude);
        Assert.Equal(-74.0060, location.Longitude);
        Assert.Equal(10.5, location.Altitude);
        Assert.Equal(45.0, location.Course);
        Assert.Equal(25.0, location.Speed);
        Assert.Equal(timestamp, location.Timestamp);
    }

    [Fact]
    public void GLocation_DefaultTimestamp_IsUtcNow()
    {
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
        var location = new GLocation { Latitude = 40.7128, Longitude = -74.0060 };
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(location.Timestamp, beforeCreation, afterCreation);
    }

    [Fact]
    public void GLocation_DefaultOptionalProperties_AreZero()
    {
        var location = new GLocation { Latitude = 40.7128, Longitude = -74.0060 };

        Assert.Equal(0, location.Altitude);
        Assert.Equal(0, location.Course);
        Assert.Equal(0, location.Speed);
    }

    [Fact]
    public void PlaceType_HasCorrectValues()
    {
        Assert.Equal(0, (int)PlaceType.Unspecified);
        Assert.Equal(1, (int)PlaceType.Saved);
        Assert.Equal(2, (int)PlaceType.Recent);
        Assert.Equal(3, (int)PlaceType.Stop);
    }

    [Fact]
    public void DriverLocation_ToLocation_ConvertsCorrectly()
    {
        var timestamp = DateTime.UtcNow;
        var driverLocation = new DriverLocation
        {
            DriverId = 1,
            DriverName = "John Doe",
            Latitude = 40.7128,
            Longitude = -74.0060,
            Altitude = 10.5,
            Course = 45.0,
            Speed = 25.0,
            Timestamp = timestamp
        };

        var location = driverLocation.ToLocation();

        Assert.Equal(driverLocation.Latitude, location.Latitude);
        Assert.Equal(driverLocation.Longitude, location.Longitude);
        Assert.Equal(driverLocation.Altitude, location.Altitude);
        Assert.Equal(driverLocation.Course, location.Course);
        Assert.Equal(driverLocation.Speed, location.Speed);
        Assert.Equal(driverLocation.Timestamp, location.Timestamp);
    }

    [Fact]
    public void DriverLocation_WithAllProperties_CreatesSuccessfully()
    {
        var trip = new Trip { Stops = [] };
        var driverLocation = new DriverLocation
        {
            Id = 1,
            DriverId = 100,
            DriverName = "Jane Smith",
            DriverState = DriverState.Available,
            Trip = trip,
            Latitude = 40.7128,
            Longitude = -74.0060,
            Altitude = 5.0,
            Course = 90.0,
            Speed = 30.0
        };

        Assert.Equal(1, driverLocation.Id);
        Assert.Equal(100, driverLocation.DriverId);
        Assert.Equal("Jane Smith", driverLocation.DriverName);
        Assert.Equal(DriverState.Available, driverLocation.DriverState);
        Assert.Equal(trip, driverLocation.Trip);
    }

    [Fact]
    public void DriverLocationList_CanBeCreated_WithLocations()
    {
        var locations = new List<DriverLocation>
        {
            new() { DriverId = 1, DriverName = "Driver 1", Latitude = 40.0, Longitude = -74.0 },
            new() { DriverId = 2, DriverName = "Driver 2", Latitude = 41.0, Longitude = -75.0 }
        };

        var locationList = new DriverLocationList(locations);

        Assert.NotNull(locationList.Locations);
        Assert.Equal(2, locationList.Locations.Count);
    }

    [Fact]
    public void DriverLocationList_DefaultConstructor_CreatesEmpty()
    {
        var locationList = new DriverLocationList();

        Assert.Null(locationList.Locations);
    }
}

