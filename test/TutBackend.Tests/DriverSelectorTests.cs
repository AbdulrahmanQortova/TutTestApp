using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TutBackend.Data;
using TutBackend.Repositories;
using TutBackend.Services;
using Tut.Common.Models;

namespace TutBackend.Tests;

public class DriverSelectorTests
{
    private IServiceProvider CreateServiceProvider(TutDbContext context)
    {
        var services = new ServiceCollection();
        services.AddScoped<TutDbContext>(_ => context);
        services.AddScoped<IDriverLocationRepository, DriverLocationRepository>();
        services.AddScoped<IDriverRepository, DriverRepository>();
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private TutDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TutDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TutDbContext(options);
    }

    [Fact]
    public async Task FindBestDriverIdAsync_WithNoLocations_ReturnsMinusOne()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var serviceProvider = CreateServiceProvider(context);
        var logger = Substitute.For<ILogger<DriverSelector>>();
        var selector = new DriverSelector(serviceProvider, logger);

        var trip = new Trip
        {
            Stops = new List<Place>
            {
                new() { Latitude = 30.0, Longitude = 31.0, PlaceType = PlaceType.Stop }
            }
        };

        // Act
        var result = await selector.FindBestDriverIdAsync(trip, new HashSet<int>());

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public async Task FindBestDriverIdAsync_WithNoStops_ReturnsMinusOne()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var serviceProvider = CreateServiceProvider(context);
        var logger = Substitute.For<ILogger<DriverSelector>>();
        var selector = new DriverSelector(serviceProvider, logger);

        var trip = new Trip { Stops = [] };

        // Act
        var result = await selector.FindBestDriverIdAsync(trip, new HashSet<int>());

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public async Task FindBestDriverIdAsync_SelectsClosestAvailableDriver()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var driverRepo = new DriverRepository(context);
        var locationRepo = new DriverLocationRepository(context);

        var driver1 = await driverRepo.AddAsync(new Driver
        {
            Mobile = "1111111111",
            FirstName = "Driver1",
            LastName = "Test",
            State = DriverState.Available
        });
        await driverRepo.SetDriverStateAsync(driver1.Id, DriverState.Available);
        var driver2 = await driverRepo.AddAsync(new Driver
        {
            Mobile = "2222222222",
            FirstName = "Driver2",
            LastName = "Test",
            State = DriverState.Available
        });
        await driverRepo.SetDriverStateAsync(driver2.Id, DriverState.Available);

        // Driver1 is closer (30.01, 31.01) to pickup (30.0, 31.0)
        await locationRepo.SaveDriverLocationAsync(new DriverLocation
        {
            DriverId = driver1.Id,
            DriverName = driver1.FullName,
            DriverState = DriverState.Available,
            Latitude = 30.01,
            Longitude = 31.01,
            Timestamp = DateTime.UtcNow
        });

        // Driver2 is farther (30.1, 31.1)
        await locationRepo.SaveDriverLocationAsync(new DriverLocation
        {
            DriverId = driver2.Id,
            DriverName = driver2.FullName,
            DriverState = DriverState.Available,
            Latitude = 30.1,
            Longitude = 31.1,
            Timestamp = DateTime.UtcNow
        });

        var serviceProvider = CreateServiceProvider(context);
        var logger = Substitute.For<ILogger<DriverSelector>>();
        var selector = new DriverSelector(serviceProvider, logger);

        var trip = new Trip
        {
            Stops = new List<Place>
            {
                new() { Latitude = 30.0, Longitude = 31.0, PlaceType = PlaceType.Stop }
            }
        };

        // Act
        var result = await selector.FindBestDriverIdAsync(trip, new HashSet<int>());

        // Assert
        Assert.Equal(driver1.Id, result);
    }

    [Fact]
    public async Task FindBestDriverIdAsync_ExcludesDriversInExcludedSet()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var driverRepo = new DriverRepository(context);
        var locationRepo = new DriverLocationRepository(context);

        var driver1 = await driverRepo.AddAsync(new Driver
        {
            Mobile = "1111111111",
            FirstName = "Driver1",
            LastName = "Test",
            State = DriverState.Available
        });
        await driverRepo.SetDriverStateAsync(driver1.Id, DriverState.Available);

        var driver2 = await driverRepo.AddAsync(new Driver
        {
            Mobile = "2222222222",
            FirstName = "Driver2",
            LastName = "Test",
            State = DriverState.Available
        });
        await driverRepo.SetDriverStateAsync(driver2.Id, DriverState.Available);

        await locationRepo.SaveDriverLocationAsync(new DriverLocation
        {
            DriverId = driver1.Id,
            DriverName = driver1.FullName,
            DriverState = DriverState.Available,
            Latitude = 30.01,
            Longitude = 31.01,
            Timestamp = DateTime.UtcNow
        });

        await locationRepo.SaveDriverLocationAsync(new DriverLocation
        {
            DriverId = driver2.Id,
            DriverName = driver2.FullName,
            DriverState = DriverState.Available,
            Latitude = 30.1,
            Longitude = 31.1,
            Timestamp = DateTime.UtcNow
        });

        var serviceProvider = CreateServiceProvider(context);
        var logger = Substitute.For<ILogger<DriverSelector>>();
        var selector = new DriverSelector(serviceProvider, logger);

        var trip = new Trip
        {
            Stops = new List<Place>
            {
                new() { Latitude = 30.0, Longitude = 31.0, PlaceType = PlaceType.Stop }
            }
        };

        // Act
        var result = await selector.FindBestDriverIdAsync(trip, new HashSet<int> { driver1.Id });

        // Assert
        Assert.Equal(driver2.Id, result);
    }

    [Fact]
    public async Task FindBestDriverIdAsync_IgnoresNonAvailableDrivers()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var driverRepo = new DriverRepository(context);
        var locationRepo = new DriverLocationRepository(context);

        var driver1 = await driverRepo.AddAsync(new Driver
        {
            Mobile = "1111111111",
            FirstName = "Driver1",
            LastName = "Test",
            State = DriverState.OnTrip
        });
        await driverRepo.SetDriverStateAsync(driver1.Id, DriverState.OnTrip);

        var driver2 = await driverRepo.AddAsync(new Driver
        {
            Mobile = "2222222222",
            FirstName = "Driver2",
            LastName = "Test",
            State = DriverState.Available
        });
        await driverRepo.SetDriverStateAsync(driver2.Id, DriverState.Available);
        
        // Driver1 is closer but not available
        await locationRepo.SaveDriverLocationAsync(new DriverLocation
        {
            DriverId = driver1.Id,
            DriverName = driver1.FullName,
            DriverState = DriverState.OnTrip,
            Latitude = 30.01,
            Longitude = 31.01,
            Timestamp = DateTime.UtcNow
        });

        // Driver2 is farther but available
        await locationRepo.SaveDriverLocationAsync(new DriverLocation
        {
            DriverId = driver2.Id,
            DriverName = driver2.FullName,
            DriverState = DriverState.Available,
            Latitude = 30.1,
            Longitude = 31.1,
            Timestamp = DateTime.UtcNow
        });

        var serviceProvider = CreateServiceProvider(context);
        var logger = Substitute.For<ILogger<DriverSelector>>();
        var selector = new DriverSelector(serviceProvider, logger);

        var trip = new Trip
        {
            Stops = new List<Place>
            {
                new() { Latitude = 30.0, Longitude = 31.0, PlaceType = PlaceType.Stop }
            }
        };

        // Act
        var result = await selector.FindBestDriverIdAsync(trip, new HashSet<int>());

        // Assert
        Assert.Equal(driver2.Id, result);
    }

    [Fact]
    public async Task FindBestDriverAsync_ReturnsDriverEntity()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var driverRepo = new DriverRepository(context);
        var locationRepo = new DriverLocationRepository(context);

        var driver = await driverRepo.AddAsync(new Driver
        {
            Mobile = "1111111111",
            FirstName = "TestDriver",
            LastName = "Last",
            State = DriverState.Available
        });
        await driverRepo.SetDriverStateAsync(driver.Id, DriverState.Available);

        await locationRepo.SaveDriverLocationAsync(new DriverLocation
        {
            DriverId = driver.Id,
            DriverName = driver.FullName,
            DriverState = DriverState.Available,
            Latitude = 30.01,
            Longitude = 31.01,
            Timestamp = DateTime.UtcNow
        });

        var serviceProvider = CreateServiceProvider(context);
        var logger = Substitute.For<ILogger<DriverSelector>>();
        var selector = new DriverSelector(serviceProvider, logger);

        var trip = new Trip
        {
            Stops = new List<Place>
            {
                new() { Latitude = 30.0, Longitude = 31.0, PlaceType = PlaceType.Stop }
            }
        };

        // Act
        var result = await selector.FindBestDriverAsync(trip, new HashSet<int>());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(driver.Id, result.Id);
        Assert.Equal("TestDriver", result.FirstName);
    }

    [Fact]
    public async Task FindBestDriverAsync_WithNoMatch_ReturnsNull()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var serviceProvider = CreateServiceProvider(context);
        var logger = Substitute.For<ILogger<DriverSelector>>();
        var selector = new DriverSelector(serviceProvider, logger);

        var trip = new Trip
        {
            Stops = new List<Place>
            {
                new() { Latitude = 30.0, Longitude = 31.0, PlaceType = PlaceType.Stop }
            }
        };

        // Act
        var result = await selector.FindBestDriverAsync(trip, new HashSet<int>());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindBestDriverIdAsync_WithCustomCostFunction_UsesIt()
    {
        // Arrange
        DriverCache.Clear();
        await using var context = CreateInMemoryContext();
        var driverRepo = new DriverRepository(context);
        var locationRepo = new DriverLocationRepository(context);

        var driver1 = await driverRepo.AddAsync(new Driver
        {
            Mobile = "1111111111",
            FirstName = "Driver1",
            LastName = "Test",
            State = DriverState.Available
        });
        await driverRepo.SetDriverStateAsync(driver1.Id, DriverState.Available);

        await locationRepo.SaveDriverLocationAsync(new DriverLocation
        {
            DriverId = driver1.Id,
            DriverName = driver1.FullName,
            DriverState = DriverState.Available,
            Latitude = 30.0,
            Longitude = 31.0,
            Timestamp = DateTime.UtcNow
        });

        var serviceProvider = CreateServiceProvider(context);
        var logger = Substitute.For<ILogger<DriverSelector>>();
        var selector = new DriverSelector(serviceProvider, logger);

        var trip = new Trip
        {
            Stops = new List<Place>
            {
                new() { Latitude = 30.0, Longitude = 31.0, PlaceType = PlaceType.Stop }
            }
        };

        // Custom cost function that always returns 100
        Func<GLocation, GLocation, double> customCost = (_, _) => 100.0;

        // Act
        var result = await selector.FindBestDriverIdAsync(trip, new HashSet<int>(), customCost);

        // Assert
        Assert.Equal(driver1.Id, result);
    }
}
