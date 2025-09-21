using Microsoft.EntityFrameworkCore;
using Tut.Common.Models;


namespace TutBackend.Data;

public class TutDbContext : DbContext
{
    // Added constructor to allow passing DbContextOptions (e.g. InMemory for tests)
    public TutDbContext(DbContextOptions<TutDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Only configure SQL Server when no options have been provided (e.g. in production)
        if (!optionsBuilder.IsConfigured)
        {
            string connectionString = Program.ConnectionString;
            Console.WriteLine(connectionString);
            optionsBuilder.UseSqlServer(connectionString)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors();
        }
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Driver> Drivers { get; set; }
    public DbSet<GLocation> Locations { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<SavedPlace> SavedPlaces { get; set; }
    public DbSet<GMessage> Messages { get; set; }
    public DbSet<Stop> Stops { get; set; }
    public DbSet<DriverLocation> DriverLocations { get; set; }


}
