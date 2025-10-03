using Grpc.Net.Client;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;
using System.Threading.Channels;
using Tut.Common.GServices;
using Tut.Common.Models;
using Tut.Common.Utils;
namespace Tut.Agents;

public class DriverAgent
{
    private readonly int _driverId;
    private readonly Options _options;
    private readonly IGDriverLocationService _locationService;
    private GLocation _currentLocation;
    private CancellationTokenSource _runCts = new();
    private Channel<DriverLocation>? _locationChannel; 
    
    
    
    public DriverAgent(int driverId) : this(driverId, new Options()) { }
    public DriverAgent(int driverId, Options options)
    {
        _driverId = driverId;
        _options = options;
        GrpcClientFactory.AllowUnencryptedHttp2 = true;
        GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:5040");
        _locationService = channel.CreateGrpcService<IGDriverLocationService>();
        _currentLocation = RandomLocationInside(options.WanderBottomLeft, options.WanderTopRight);
    }


    public void Start()
    {
        _locationChannel = Channel.CreateBounded<DriverLocation>(new BoundedChannelOptions(20)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
        _channelCompleted = false;
        _locationService.RegisterLocation(_locationChannel.AsAsyncEnumerable());
        _runCts = new CancellationTokenSource();
        _ = Task.Run(() => RunLoop(_runCts.Token));
    }

    public void Stop()
    {
        _channelCompleted = true;
        _locationChannel?.Writer.Complete();
        _runCts.Cancel();
    }


    private async Task RunLoop(CancellationToken ct)
    {
        CancellationTokenSource wanderCts = new ();
        while (!ct.IsCancellationRequested)
        {
            await WanderLoop(wanderCts.Token);
        }
        await wanderCts.CancelAsync();
    }

    private Task TripProcess(Trip trip)
    {
        return Task.CompletedTask;
    }

    private bool _channelCompleted;
    private async Task NotifyLocationChanged()
    {
        if (!_channelCompleted && _locationChannel is not null)
            await _locationChannel.Writer.WriteAsync(new DriverLocation
            {
                DriverId = _driverId,
                Location = _currentLocation
            });
    }
    
    private async Task WanderLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            GLocation destLocation = RandomLocationInside(_options.WanderBottomLeft, _options.WanderTopRight);
            await MoveTo(destLocation, ct);
        }
    }


    private async Task MoveTo(GLocation destLocation, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && !LocationUtils.SameLocation(_currentLocation, destLocation))
        {
            await StepTowards(destLocation, _options.Speed, ct);
        }
    }

    private async Task StepTowards(GLocation location, int speed, CancellationToken ct)
    {
        // Calculate meters to move during the 5 second interval.
        // Assumes 'speed' is in km/h. 1 km/h == 1000/3600 m/s. Over 5s -> meters = speed * 1000/3600 * 5 == speed * (5.0/3.6)
        double metersToMove = speed * (5.0 / 3.6);

        // Compute the next location using the utility (which accepts meters)
        var next = LocationUtils.Towards(_currentLocation, location, metersToMove);

        // Compute course (bearing) from current to next. We treat 0/360 as North and measure clockwise.
        double dLat = next.Latitude - _currentLocation.Latitude;
        double dLon = next.Longitude - _currentLocation.Longitude;
        double angleRad = Math.Atan2(dLon, dLat); // note: lon first to get bearing from north
        double course = angleRad * (180.0 / Math.PI);
        if (course < 0) course += 360.0;

        // Update the current location in-place
        _currentLocation.Latitude = next.Latitude;
        _currentLocation.Longitude = next.Longitude;
        _currentLocation.Course = course;
        _currentLocation.Speed = speed;
        _currentLocation.Timestamp = DateTime.UtcNow;

        await NotifyLocationChanged();
        
        // Simulate the passage of 5 seconds for this step
        await Task.Delay(TimeSpan.FromSeconds(5), ct);
    }


    private static GLocation RandomLocationInside(GLocation bottomLeft, GLocation topRight)
    {
        if (bottomLeft is null) throw new ArgumentNullException(nameof(bottomLeft));
        if (topRight is null) throw new ArgumentNullException(nameof(topRight));

        // Normalize bounds in case the caller provided them inverted
        double minLat = Math.Min(bottomLeft.Latitude, topRight.Latitude);
        double maxLat = Math.Max(bottomLeft.Latitude, topRight.Latitude);
        double minLon = Math.Min(bottomLeft.Longitude, topRight.Longitude);
        double maxLon = Math.Max(bottomLeft.Longitude, topRight.Longitude);

        // Use the thread-safe shared Random instance (available on .NET Core / .NET 5+)
        var rng = Random.Shared;

        double lat = rng.NextDouble() * (maxLat - minLat) + minLat;
        double lon = rng.NextDouble() * (maxLon - minLon) + minLon;

        return new GLocation
        {
            Latitude = lat,
            Longitude = lon,
            Altitude = 0,
            Course = 0,
            Speed = 0,
            Timestamp = DateTime.UtcNow
        };
    }
    
    public class Options
    {
        public int ArrivalWaitTimeSeconds { get; set; } = 20;
        public int StopWaitTimeSeconds { get; set; } = 20;
        public int PaymentWaitTimeSeconds { get; set; } = 20;
        public int Speed { get; set; } = 70;
        public readonly GLocation WanderBottomLeft = new GLocation { Latitude = 30, Longitude = 31.15 };
        public readonly GLocation WanderTopRight = new GLocation { Latitude = 30.15, Longitude = 31.5 };
    }
}
