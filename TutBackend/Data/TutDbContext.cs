using Microsoft.EntityFrameworkCore;


namespace TutBackend.Data;

public class TutDbContext : DbContext
{



    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string connectionString = Program.ConnectionString;
        optionsBuilder.UseSqlServer(connectionString)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
    }


}
