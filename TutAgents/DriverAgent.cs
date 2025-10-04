using Grpc.Core;
using Grpc.Net.Client;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;
using System.Threading.Channels;
using Tut.Common.GServices;
using Tut.Common.Models;
using Tut.Common.Utils;
using System.Threading;
using Tut.Common.Managers;
namespace Tut.Agents;

public class DriverAgent
{
    private readonly string _username;
    private readonly string _password;
    private readonly Options _options;
    private GLocation _currentLocation;
    private CancellationTokenSource _runCts = new();
    
    private readonly DriverLocationManagerService _locationManager;
    private readonly DriverTripManager _tripManager;
    
    public DriverAgent(string username, string password) : this(username, password, new Options()) { }
    public DriverAgent(string username, string password, Options options)
    {
        _username = username;
        _password = password;
        _options = options;
        GrpcClientFactory.AllowUnencryptedHttp2 = true;
        GrpcChannelFactory factory = new GrpcChannelFactory("http://localhost:5040");
        _currentLocation = RandomLocationInside(options.WanderBottomLeft, options.WanderTopRight);
        _locationManager = new DriverLocationManagerService(username, factory);
        _tripManager = new DriverTripManager(username, factory);
    }


    public void Start()
    {
        _runCts = new CancellationTokenSource();
        _locationManager.ErrorReceived += (s, e) => Console.WriteLine("LM> " + e.ErrorText);
        _tripManager.ErrorReceived += (s, e) => Console.WriteLine("TM> " + e.ErrorText);
        _tripManager.ConnectionStateChanged += (s, e) => Console.WriteLine("TM> " + e.NewState);
        _tripManager.OfferReceived += async (s, e) =>
        {
            if (_wanderCts is not null)
            {
                await _wanderCts.CancelAsync();
                _wanderCts.Dispose();
                _wanderCts = null;
            }
            await _tripManager.SendTripReceivedAsync();
            await Task.Delay(2000);
            await _tripManager.SendAcceptTripAsync();
        };
        _tripManager.StatusChanged += async (s, e) => await HandleStatusUpdate(e.Trip);
        _ = _locationManager.Connect(_runCts.Token, TimeSpan.FromSeconds(2));
        _ = _tripManager.Connect(_runCts.Token);
        _ = _tripManager.SendPunchInAsync(_runCts.Token);
    }

    private int _lastStop;
    private async Task HandleStatusUpdate(Trip? trip)
    {
        CancellationTokenSource onTripMovementCancellation = new();
        if (trip is null)
        {
            if (_wanderCts is null)
            {
                _wanderCts = new CancellationTokenSource();
                _ = WanderLoop(_wanderCts.Token);
            }
            return;
        }
        switch (trip.Status)
        {
            case TripState.Accepted:
                await MoveTo(trip.Stops[0].Place.Location, onTripMovementCancellation.Token);
                if(!onTripMovementCancellation.IsCancellationRequested)
                    await _tripManager.SendArrivedAtPickupAsync(onTripMovementCancellation.Token);
                break;
            case TripState.DriverArrived:
                await Task.Delay(TimeSpan.FromSeconds(_options.ArrivalWaitTimeSeconds), onTripMovementCancellation.Token);
                if (!onTripMovementCancellation.IsCancellationRequested)
                    await MoveTo(trip.Stops[1].Place.Location, onTripMovementCancellation.Token);
                if (!onTripMovementCancellation.IsCancellationRequested)
                {
                    if (trip.Stops.Count > 2)
                        await _tripManager.SendArrivedAtStopAsync(onTripMovementCancellation.Token);
                    else
                        await _tripManager.SendArrivedAtDestinationAsync(onTripMovementCancellation.Token);
                }
                break;
            case TripState.AtStop1:
            case TripState.AtStop2:
            case TripState.AtStop3:
            case TripState.AtStop4:
            case TripState.AtStop5:
                await Task.Delay(TimeSpan.FromSeconds(_options.StopWaitTimeSeconds), onTripMovementCancellation.Token);
                if (!onTripMovementCancellation.IsCancellationRequested)
                    await _tripManager.SendContinueTripAsync(onTripMovementCancellation.Token);
                break;
            case TripState.AfterStop1:
                if (!onTripMovementCancellation.IsCancellationRequested)
                    await MoveTo(trip.Stops[2].Place.Location, onTripMovementCancellation.Token);
                if (!onTripMovementCancellation.IsCancellationRequested)
                {
                    if (trip.Stops.Count > 3)
                        await _tripManager.SendArrivedAtStopAsync(onTripMovementCancellation.Token);
                    else
                        await _tripManager.SendArrivedAtDestinationAsync(onTripMovementCancellation.Token);
                }
                break;
            case TripState.AfterStop2:
                if (!onTripMovementCancellation.IsCancellationRequested)
                    await MoveTo(trip.Stops[3].Place.Location, onTripMovementCancellation.Token);
                if (!onTripMovementCancellation.IsCancellationRequested)
                {
                    if (trip.Stops.Count > 4)
                        await _tripManager.SendArrivedAtStopAsync(onTripMovementCancellation.Token);
                    else
                        await _tripManager.SendArrivedAtDestinationAsync(onTripMovementCancellation.Token);
                }
                break;
            case TripState.AfterStop3:
                if (!onTripMovementCancellation.IsCancellationRequested)
                    await MoveTo(trip.Stops[4].Place.Location, onTripMovementCancellation.Token);
                if (!onTripMovementCancellation.IsCancellationRequested)
                {
                    if (trip.Stops.Count > 5)
                        await _tripManager.SendArrivedAtStopAsync(onTripMovementCancellation.Token);
                    else
                        await _tripManager.SendArrivedAtDestinationAsync(onTripMovementCancellation.Token);
                }
                break;
            case TripState.AfterStop4:
                if (!onTripMovementCancellation.IsCancellationRequested)
                    await MoveTo(trip.Stops[5].Place.Location, onTripMovementCancellation.Token);
                if (!onTripMovementCancellation.IsCancellationRequested)
                {
                    if (trip.Stops.Count > 6)
                        await _tripManager.SendArrivedAtStopAsync(onTripMovementCancellation.Token);
                    else
                        await _tripManager.SendArrivedAtDestinationAsync(onTripMovementCancellation.Token);
                }
                break;
            case TripState.AfterStop5:
                if (!onTripMovementCancellation.IsCancellationRequested)
                    await MoveTo(trip.Stops[6].Place.Location, onTripMovementCancellation.Token);
                if (!onTripMovementCancellation.IsCancellationRequested)
                    await _tripManager.SendArrivedAtDestinationAsync(onTripMovementCancellation.Token);
                break;
            case TripState.Arrived:
                if (!onTripMovementCancellation.IsCancellationRequested)
                    await Task.Delay(TimeSpan.FromMicroseconds(_options.PaymentWaitTimeSeconds), onTripMovementCancellation.Token);
                if (!onTripMovementCancellation.IsCancellationRequested)
                    await _tripManager.SendCashPaymentMadeAsync((int)trip.ActualCost, onTripMovementCancellation.Token);
                break;
        }
    }

    public void Stop()
    {
        _ = _locationManager.Disconnect();
        _runCts.Cancel();
        _runCts.Dispose();
    }

    private CancellationTokenSource? _wanderCts;

    private async Task NotifyLocationChanged()
    {
        _locationManager.RegisterLocation(_currentLocation);
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
        public int Speed { get; set; } = 200;
        public readonly GLocation WanderBottomLeft = new GLocation { Latitude = 30, Longitude = 31.15 };
        public readonly GLocation WanderTopRight = new GLocation { Latitude = 30.15, Longitude = 31.5 };
    }
}
