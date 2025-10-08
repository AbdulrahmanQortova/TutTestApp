using ProtoBuf.Grpc.Server;
using TutBackend.Data;
using TutBackend.Repositories;
using TutBackend.Services;
namespace TutBackend;

public static class Program
{
    public static string ConnectionString { get; private set; } = string.Empty;

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        string connectionString = builder.Configuration.GetConnectionString("PegasusCon") ?? "";
        ConnectionString = connectionString;

        
        builder.Services.AddCodeFirstGrpc(config =>
        {
            config.ResponseCompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
        });
        builder.Services.AddDbContext<TutDbContext>();

        // Register repositories
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IDriverRepository, DriverRepository>();
        builder.Services.AddScoped<ILocationRepository, LocationRepository>();
        builder.Services.AddScoped<ITripRepository, TripRepository>();
        builder.Services.AddScoped<IPlaceRepository, PlaceRepository>();
        builder.Services.AddScoped<IMessageRepository, MessageRepository>();
        builder.Services.AddScoped<IDriverLocationRepository, DriverLocationRepository>();
        builder.Services.AddScoped<QipClient>();

        // Register DriverSelector for injecting into TripDistributor
        builder.Services.AddTransient<DriverSelector>();

        // Register TripDistributor as a hosted background service
        builder.Services.AddHostedService<TripDistributor>();

        // Read Qip base address from configuration in a null-safe way
        var qipBaseAddress = builder.Configuration.GetValue<string>("Qip:BaseAddress");
        if (string.IsNullOrWhiteSpace(qipBaseAddress))
        {
            throw new InvalidOperationException("Configuration value 'Qip:BaseAddress' is missing. Please set it in appsettings.json or environment variables.");
        }

        builder.Services.AddHttpClient<QipClient>(client =>
        {
            client.BaseAddress = new Uri(qipBaseAddress);
        });

        var app = builder.Build();

// Configure the HTTP request pipeline.
        app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
        app.MapGrpcService<GDriverManagerService>();
        app.MapGrpcService<GDriverLocationService>();
        app.MapGrpcService<GTripManagerService>();
        app.MapGrpcService<GDriverTripService>();
        app.MapGrpcService<GUserTripService>();
        app.Run();
    }
}
// Add services to the container.
