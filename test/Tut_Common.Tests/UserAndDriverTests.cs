using Tut.Common.Models;
using Xunit;

namespace Tut.Common.Tests;

public class UserAndDriverTests
{
    [Fact]
    public void User_FullName_CombinesFirstAndLastName()
    {
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe"
        };

        Assert.Equal("John Doe", user.FullName);
    }

    [Fact]
    public void User_WithAllProperties_CreatesSuccessfully()
    {
        var places = new List<Place>
        {
            new() { Latitude = 40.7128, Longitude = -74.0060, PlaceType = PlaceType.Saved, Order = 0 }
        };
        
        var user = new User
        {
            Id = 1,
            Mobile = "+1234567890",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Status = UserState.Active,
            Password = "hashedPassword",
            TotalTrips = 25,
            Rating = 4.8,
            TotalSpending = 1250.50,
            SavedPlaces = places
        };

        Assert.Equal(1, user.Id);
        Assert.Equal("+1234567890", user.Mobile);
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("john.doe@example.com", user.Email);
        Assert.Equal(UserState.Active, user.Status);
        Assert.Equal("hashedPassword", user.Password);
        Assert.Equal(25, user.TotalTrips);
        Assert.Equal(4.8, user.Rating);
        Assert.Equal(1250.50, user.TotalSpending);
        Assert.Single(user.SavedPlaces);
    }

    [Fact]
    public void User_DefaultValues_AreSetCorrectly()
    {
        var user = new User();

        Assert.Equal(string.Empty, user.Mobile);
        Assert.Equal(string.Empty, user.FirstName);
        Assert.Equal(string.Empty, user.LastName);
        Assert.Equal(string.Empty, user.Email);
        Assert.Equal(UserState.Unspecified, user.Status);
        Assert.Equal(string.Empty, user.Password);
        Assert.Equal(0, user.TotalTrips);
        Assert.Equal(0, user.Rating);
        Assert.Equal(0, user.TotalSpending);
        Assert.NotNull(user.SavedPlaces);
        Assert.Empty(user.SavedPlaces);
    }

    [Fact]
    public void UserState_HasCorrectValues()
    {
        Assert.Equal(0, (int)UserState.Unspecified);
        Assert.Equal(1, (int)UserState.Active);
        Assert.Equal(2, (int)UserState.OnTrip);
        Assert.Equal(3, (int)UserState.Blocked);
        Assert.Equal(4, (int)UserState.Deleted);
    }

    [Fact]
    public void Driver_FullName_CombinesFirstAndLastName()
    {
        var driver = new Driver
        {
            FirstName = "Jane",
            LastName = "Smith"
        };

        Assert.Equal("Jane Smith", driver.FullName);
    }

    [Fact]
    public void Driver_WithAllProperties_CreatesSuccessfully()
    {
        var driver = new Driver
        {
            Id = 1,
            Mobile = "+1987654321",
            Email = "jane.smith@example.com",
            FirstName = "Jane",
            LastName = "Smith",
            NationalId = "ABC123456",
            State = DriverState.Available,
            Password = "hashedPassword",
            TotalTrips = 150,
            Rating = 4.9,
            TotalEarnings = 15000.75
        };

        Assert.Equal(1, driver.Id);
        Assert.Equal("+1987654321", driver.Mobile);
        Assert.Equal("jane.smith@example.com", driver.Email);
        Assert.Equal("Jane", driver.FirstName);
        Assert.Equal("Smith", driver.LastName);
        Assert.Equal("ABC123456", driver.NationalId);
        Assert.Equal(DriverState.Available, driver.State);
        Assert.Equal("hashedPassword", driver.Password);
        Assert.Equal(150, driver.TotalTrips);
        Assert.Equal(4.9, driver.Rating);
        Assert.Equal(15000.75, driver.TotalEarnings);
    }

    [Fact]
    public void Driver_DefaultValues_AreSetCorrectly()
    {
        var driver = new Driver();

        Assert.Equal(string.Empty, driver.Mobile);
        Assert.Equal(string.Empty, driver.Email);
        Assert.Equal(string.Empty, driver.FirstName);
        Assert.Equal(string.Empty, driver.LastName);
        Assert.Equal(string.Empty, driver.NationalId);
        Assert.Equal(DriverState.Unspecified, driver.State);
        Assert.Equal(string.Empty, driver.Password);
        Assert.Equal(0, driver.TotalTrips);
        Assert.Equal(0, driver.Rating);
        Assert.Equal(0, driver.TotalEarnings);
    }

    [Fact]
    public void DriverState_HasCorrectValues()
    {
        Assert.Equal(0, (int)DriverState.Unspecified);
        Assert.Equal(1, (int)DriverState.Offline);
        Assert.Equal(2, (int)DriverState.Inactive);
        Assert.Equal(3, (int)DriverState.Available);
        Assert.Equal(4, (int)DriverState.Requested);
        Assert.Equal(5, (int)DriverState.EnRoute);
        Assert.Equal(6, (int)DriverState.OnTrip);
        Assert.Equal(7, (int)DriverState.Deleted);
    }

    [Fact]
    public void DriverList_CanBeCreated_WithDrivers()
    {
        var drivers = new List<Driver>
        {
            new() { FirstName = "Driver", LastName = "One" },
            new() { FirstName = "Driver", LastName = "Two" }
        };

        var driverList = new DriverList(drivers);

        Assert.NotNull(driverList.Drivers);
        Assert.Equal(2, driverList.Drivers.Count);
    }

    [Fact]
    public void DriverList_DefaultConstructor_CreatesEmpty()
    {
        var driverList = new DriverList();

        Assert.Null(driverList.Drivers);
    }

    [Fact]
    public void User_FullName_WithEmptyNames_ReturnsSpace()
    {
        var user = new User
        {
            FirstName = "",
            LastName = ""
        };

        Assert.Equal(" ", user.FullName);
    }

    [Fact]
    public void Driver_FullName_WithEmptyNames_ReturnsSpace()
    {
        var driver = new Driver
        {
            FirstName = "",
            LastName = ""
        };

        Assert.Equal(" ", driver.FullName);
    }
}

