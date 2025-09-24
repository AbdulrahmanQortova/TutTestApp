using Microsoft.EntityFrameworkCore;
using TutBackend.Data;
using Tut.Common.Models;

namespace TutBackend.Tests;

// This fixture seeds an in-memory database before any tests run and clears it when all tests finish.
public class TestDatabaseFixture : IAsyncLifetime
{
    public TutDbContext Context { get; }
    private readonly string _dbName = "GlobalTestDb";
    public string DatabaseName => _dbName;

    public TestDatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<TutDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;
        Context = new TutDbContext(options);
    }

    public async Task InitializeAsync()
    {
        // If already seeded, skip
        if (await Context.Drivers.AnyAsync())
            return;

        // Seed places using Cairo landmarks
        var places = new List<Place>
        {
            new Place { Name = "Tahrir Square", Address = "Tahrir Square, Downtown Cairo", Location = new GLocation { Latitude = 30.0444, Longitude = 31.2357 } },
            new Place { Name = "Cairo Tower", Address = "Gezira Island", Location = new GLocation { Latitude = 30.0459, Longitude = 31.2243 } },
            new Place { Name = "Giza Pyramids", Address = "Giza", Location = new GLocation { Latitude = 29.9792, Longitude = 31.1342 } },
            new Place { Name = "Zamalek", Address = "Gezira Island neighborhood", Location = new GLocation { Latitude = 30.0601, Longitude = 31.2231 } },
            new Place { Name = "Khan el-Khalili", Address = "Old Cairo", Location = new GLocation { Latitude = 30.0470, Longitude = 31.2625 } },
            new Place { Name = "Salah El-Din Citadel", Address = "Cairo Citadel", Location = new GLocation { Latitude = 30.0299, Longitude = 31.2619 } },
            new Place { Name = "Mohamed Ali Mosque", Address = "Citadel", Location = new GLocation { Latitude = 30.0296, Longitude = 31.2616 } },
            new Place { Name = "Al-Azhar Mosque", Address = "Al-Azhar", Location = new GLocation { Latitude = 30.0478, Longitude = 31.2625 } },
            new Place { Name = "Cairo Opera House", Address = "Gezira", Location = new GLocation { Latitude = 30.0453, Longitude = 31.2240 } },
            new Place { Name = "Al-Muizz Street", Address = "Historic Cairo", Location = new GLocation { Latitude = 30.0473, Longitude = 31.2636 } },
            new Place { Name = "Coptic Cairo (Hanging Church)", Address = "Coptic Cairo", Location = new GLocation { Latitude = 30.0355, Longitude = 31.2613 } },
            new Place { Name = "Egyptian Museum", Address = "Tahrir Square", Location = new GLocation { Latitude = 30.0478, Longitude = 31.2336 } },
        };
        await Context.AddRangeAsync(places);

        // Seed users
        var users = new List<User>()
        {
            new () {Mobile = "910123451", FirstName = "Donald", LastName = "Duck" },
            new () {Mobile = "910123452", FirstName = "Daisy", LastName = "Duck" },
            new () {Mobile = "910123453", FirstName = "Louis", LastName = "Duck" },
            new () {Mobile = "910123454", FirstName = "Mickey", LastName = "Mouse" },
            new () {Mobile = "910123455", FirstName = "Snow", LastName = "White" },
        };
        await Context.AddRangeAsync(users);

        // Seed drivers
        var drivers = new List<Driver>
        {
            new Driver { Mobile = "810123451", FirstName = "Harry", LastName = "Potter", Location = new GLocation { Latitude = 30.0444, Longitude = 31.2357 } },
            new Driver { Mobile = "810123452", FirstName = "Morticia", LastName = "Addams", Location = new GLocation { Latitude = 29.9792, Longitude = 31.1342 } },
            new Driver { Mobile = "810123453", FirstName = "John", LastName = "Snow", Location = new GLocation { Latitude = 30.0459, Longitude = 31.2243 } },
        };
        await Context.AddRangeAsync(drivers);

        await Context.SaveChangesAsync();

        Trip firstTrip = new()
        {
            User = users[0],
            Driver = drivers[0],
            RequestedDriverPlace = places[3],
            RequestingPlace = places[0],
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            Status = TripState.Ended,
            EstimatedCost = 580,
            Stops = [
                new Stop { Place = places[0] },
                new Stop { Place = places[1] },
                new Stop { Place = places[2] },
            ],
        };

        Context.Trips.Add(firstTrip);
        await Context.SaveChangesAsync();
        
        // Seed 20 trips, assign to seeded users and drivers, add stops to some trips
        var rand = new Random(0);
        var trips = new List<Trip>();
        for (int i = 0; i < 19; i++)
        {
            var user = users[i % users.Count];
            var driver = drivers[i % drivers.Count];

            // Choose requested/requesting places randomly
            var requestedPlace = places[rand.Next(places.Count)];
            var requestingPlace = places[rand.Next(places.Count)];

            var trip = new Trip
            {
                User = user,
                Driver = driver,
                RequestedDriverPlace = requestedPlace,
                RequestingPlace = requestingPlace,
                CreatedAt = DateTime.UtcNow.AddDays(-i),
                Status = TripState.Requested,
                EstimatedCost = 10 + i,
                Stops = new List<Stop>()
            };

            // Ensure at least 2 distinct stops per trip
            var chosenIndices = new HashSet<int>();
            while (chosenIndices.Count < 2)
            {
                chosenIndices.Add(rand.Next(places.Count));
            }

            // Some trips get 1-2 extra stops (about 25% chance) — deterministic via i
            int extraStops = (i % 4 == 0) ? rand.Next(1, 3) : 0; // 1 or 2 extra stops for some trips
            while (extraStops > 0)
            {
                chosenIndices.Add(rand.Next(places.Count));
                extraStops--;
            }

            foreach (var idx in chosenIndices)
            {
                trip.Stops.Add(new Stop { Place = places[idx] });
            }

            trips.Add(trip);
        }

        await Context.AddRangeAsync(trips);
        await Context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        // Clear the in-memory database after all tests
        await Context.Database.EnsureDeletedAsync();
        await Context.DisposeAsync();
    }
}

// Register the fixture as a collection so the InitializeAsync/DisposeAsync run once for the collection
[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>
{
}
