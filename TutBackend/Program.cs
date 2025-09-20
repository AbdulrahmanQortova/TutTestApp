using TutBackend.Data;
namespace TutBackend;

public static class Program
{
    public static string ConnectionString { get; private set; } = string.Empty;

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        string connectionString = builder.Configuration.GetConnectionString("PegasusCon") ?? "";
        ConnectionString = connectionString;

        
        
        builder.Services.AddGrpc();
        builder.Services.AddDbContext<TutDbContext>();
        var app = builder.Build();

// Configure the HTTP request pipeline.
        app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

        app.Run();
    }
}
// Add services to the container.
