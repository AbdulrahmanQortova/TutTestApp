using ProtoBuf.Grpc.Client;
using Tut.Common.GServices;
using Tut.Common.Models;
using Tut.Common.Utils;
using Tut.Common.Managers;

namespace Tut.Agents;

public class DriverAgent
{
    private readonly Options _options;
    private GLocation _currentLocation;
    private CancellationTokenSource _runCts = new();

    private readonly DriverLocationManagerService _locationManager;
    private readonly DriverTripManager _tripManager;

    // Hold background tasks so the compiler does not warn about unobserved tasks.

    // Map trip state to handler to reduce method complexity
    private readonly IReadOnlyDictionary<TripState, Func<Trip, CancellationToken, Task>> _stateHandlers;
    private readonly string _username;
    public DriverAgent(string username, string password) : this(username, password, new Options()) { }
    public DriverAgent(string username, string password, Options options)
    {
        _options = options;
        _username = username;
        GrpcClientFactory.AllowUnencryptedHttp2 = true;
        GrpcChannelFactory factory = new GrpcChannelFactory("http://localhost:5040");
        _currentLocation = RandomLocationInside(options.WanderBottomLeft, options.WanderTopRight);
        _locationManager = new DriverLocationManagerService(username, factory);
        _tripManager = new DriverTripManager(username, factory);

        // use password in a no-op to avoid "parameter is never used" warnings without logging sensitive data
        _ = password.Length;

        // Initialize state handlers
        var handlers = new Dictionary<TripState, Func<Trip, CancellationToken, Task>>
        {
            { TripState.Accepted, async (t, ct) => await HandleAcceptedAsync(t, ct) },
            { TripState.DriverArrived, async (_, ct) => await HandleDriverArrivedAsync(_, ct) },
            { TripState.Ongoing, async (t, ct) => await HandleOngoingAsync(t, ct) },
            { TripState.AtStop, async (_, ct) => await HandleAtStopsAsync(_, ct) },
            { TripState.Arrived, async (t, ct) => await HandleArrivedAsync(t, ct) }
        };

        _stateHandlers = handlers;
    }


    public void Start()
    {
        _runCts = new CancellationTokenSource();
        _locationManager.ErrorReceived += (_, e) => Console.WriteLine("LM> " + e.ErrorText);
        _tripManager.ErrorReceived += (_, e) => Console.WriteLine("TM> " + e.ErrorText);
        _tripManager.ConnectionStateChanged += (_, e) => Console.WriteLine("TM> " + e.NewState);

        // Offer handling: cancel wandering, acknowledge and accept the offer.
        _tripManager.OfferReceived += (_, _) => _ = HandleOfferAsync();

        // Invoke status updates as fire-and-forget. Use async void event handler to avoid discard assignment.
        _tripManager.StatusChanged += async (_, e) => await HandleStatusUpdate(e.Trip);

        // Start background connection tasks and keep references so they are not unobserved.
        _ = _locationManager.Connect(_runCts.Token, TimeSpan.FromSeconds(2))
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Console.WriteLine("Location connect failed: " + t.Exception?.GetBaseException().Message);
                }
            }, TaskContinuationOptions.ExecuteSynchronously);

        _ = _tripManager.Connect(_runCts.Token)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Console.WriteLine("Trip connect failed: " + t.Exception?.GetBaseException().Message);
                }
            }, TaskContinuationOptions.ExecuteSynchronously);

        _ = _tripManager.SendPunchInAsync(_runCts.Token)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Console.WriteLine("Punch-in failed: " + t.Exception?.GetBaseException().Message);
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
    }

    private async Task HandleOfferAsync()
    {
        try
        {
            if (_wanderCts is not null)
            {
                try
                {
                    await _wanderCts.CancelAsync();
                }
                catch (ObjectDisposedException)
                {
                    // ignore - it was disposed concurrently
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Wander cancel error: " + ex.Message);
                }

                try
                {
                    _wanderCts.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Wander dispose error: " + ex.Message);
                }
                _wanderCts = null;
            }

            Log("Sending Ack");
            await _tripManager.SendTripReceivedAsync();
            await Task.Delay(2000);
            Log("Sending Accept");
            await _tripManager.SendAcceptTripAsync();
        }
        catch (Exception ex)
        {
            Log("Exception");
            Console.WriteLine("Offer handling error: " + ex.Message);
        }
    }

    private async Task HandleStatusUpdate(Trip? trip)
    {
        // Link the on-trip CTS to the global run CTS so Stop() cancels any movement.
        using var onTripMovementCancellation = CancellationTokenSource.CreateLinkedTokenSource(_runCts.Token);
        CancellationToken ct = onTripMovementCancellation.Token;

        if (trip is null)
        {
            Log("Received Null Trip in Status, Start Wandering");
            if (_wanderCts is null && !ct.IsCancellationRequested)
            {
                _wanderCts = new CancellationTokenSource();
                _ = WanderLoop(_wanderCts.Token);
            }
            return;
        }

        Log("Received Trip Status Update: " + trip.Status);
        // Delegate the detailed status processing to keep this method simple.
        await ProcessTripStatusAsync(trip, ct);
    }

    private async Task ProcessTripStatusAsync(Trip trip, CancellationToken ct)
    {
        try
        {
            if (_stateHandlers.TryGetValue(trip.Status, out var handler))
            {
                await handler(trip, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Movement was cancelled — expected when trip changes or Stop() called.
        }
        catch (Exception ex)
        {
            Log("ProcessTripStatusAsync error: " + ex.Message);
        }
    }

    // Extracted helpers
    private Task SafeMoveToIndexAsync(Trip trip, int index, CancellationToken ct)
    {
        Log($"Moving to Stop[{index}]");
        if (index < 0 || trip.Stops.Count <= index)
            return Task.CompletedTask;
        GLocation loc = trip.Stops[index].ToLocation();
        return MoveTo(loc, ct);
    }

    private Task SendArriveOrStopAsync(Trip trip, CancellationToken ct)
    {
        Log("Sending Arrived to Stop / Destination");
        return (trip.NextStop < trip.Stops.Count -1)
            ? _tripManager.SendArrivedAtStopAsync(ct)
            : _tripManager.SendArrivedAtDestinationAsync(ct);
    }

    private async Task HandleAcceptedAsync(Trip trip, CancellationToken ct)
    {
        Log("Handling Accept, Moving to Pickup");
        await SafeMoveToIndexAsync(trip, 0, ct);
        if (!ct.IsCancellationRequested)
            await _tripManager.SendArrivedAtPickupAsync(ct);
    }

    private async Task HandleDriverArrivedAsync(Trip _, CancellationToken ct)
    {
        Log("Handling Driver Arrived, Waiting, then Sending StartTrip");
        await Task.Delay(TimeSpan.FromSeconds(_options.ArrivalWaitTimeSeconds), ct);
        if (!ct.IsCancellationRequested)
            await _tripManager.SendStartTripAsync(ct);
    }

    private async Task HandleAtStopsAsync(Trip _, CancellationToken ct)
    {
        Log("Handling At Stop, Waiting, then Sending ContinueTrip");
        await Task.Delay(TimeSpan.FromSeconds(_options.StopWaitTimeSeconds), ct);
        if (!ct.IsCancellationRequested)
            await _tripManager.SendContinueTripAsync(ct);
    }

    private async Task HandleOngoingAsync(Trip trip, CancellationToken ct)
    {
        Log("Handling Ongoing, Moving to Next Stop");
        await SafeMoveToIndexAsync(trip, trip.NextStop, ct);
        if (!ct.IsCancellationRequested)
            await SendArriveOrStopAsync(trip, ct);
    }
    
    
    
    private async Task HandleArrivedAsync(Trip trip, CancellationToken ct)
    {
        Log("Handling Arrived, Waiting, then Acknowledging Cash Payment");
        
        if (!ct.IsCancellationRequested)
            await Task.Delay(TimeSpan.FromSeconds(_options.PaymentWaitTimeSeconds), ct);
        if (!ct.IsCancellationRequested)
            await _tripManager.SendCashPaymentMadeAsync((int)trip.ActualCost, ct);
    }

    public void Stop()
    {
        _ = _locationManager.Disconnect();
        _ = _tripManager.Disconnect();
        try
        {
            _runCts.Cancel();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Run cancellation error: " + ex.Message);
        }
        _runCts.Dispose();
        _runCts = new CancellationTokenSource();

    }

    private CancellationTokenSource? _wanderCts;

    private Task NotifyLocationChanged()
    {
        // RegisterLocation is synchronous in the manager; return a completed task so callers may await.
        try
        {
            _locationManager.RegisterLocation(_currentLocation);
        }
        catch (Exception ex)
        {
            Console.WriteLine("NotifyLocationChanged error: " + ex.Message);
        }
        return Task.CompletedTask;
    }

    private async Task WanderLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                GLocation destLocation = RandomLocationInside(_options.WanderBottomLeft, _options.WanderTopRight);
                await MoveTo(destLocation, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
        catch (Exception ex)
        {
            Console.WriteLine("WanderLoop error: " + ex.Message);
        }
    }


    private async Task MoveTo(GLocation destLocation, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && !LocationUtils.SameLocation(_currentLocation, destLocation))
            {
                await StepTowards(destLocation, _options.Speed, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
    }

    private async Task StepTowards(GLocation location, int speed, CancellationToken ct)
    {
        // Calculate meters to move during the 5-second interval.
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

        _currentLocation = new GLocation
        {
            Latitude = next.Latitude,
            Longitude = next.Longitude,
            Course = course,
            Speed = speed
        };

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

    private void Log(string str)
    {
        Console.WriteLine($"DA({_username})> {str}");
    }
    
    
    public class Options
    {
        public int ArrivalWaitTimeSeconds { get; set; } = 20;
        public int StopWaitTimeSeconds { get; set; } = 20;
        public int PaymentWaitTimeSeconds { get; set; } = 20;
        public int Speed { get; set; } = 1500;
        public readonly GLocation WanderBottomLeft = new GLocation { Latitude = 30, Longitude = 31.15 };
        public readonly GLocation WanderTopRight = new GLocation { Latitude = 30.15, Longitude = 31.5 };
    }
}

