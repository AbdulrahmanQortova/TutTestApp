using ProtoBuf.Grpc.Client;
using Tut.Common.GServices;
using Tut.Common.Models;
using Tut.Common.Utils;
using Tut.Common.Managers;

namespace Tut.Agents;

public class DriverAgent
{
    private readonly Options _options;
    private readonly GLocation _currentLocation;
    private CancellationTokenSource _runCts = new();

    private readonly DriverLocationManagerService _locationManager;
    private readonly DriverTripManager _tripManager;

    // Hold background tasks so the compiler does not warn about unobserved tasks.
    private Task? _locationConnectTask;
    private Task? _tripConnectTask;
    private Task? _punchInTask;

    // Map trip state to handler to reduce method complexity
    private readonly IReadOnlyDictionary<TripState, Func<Trip, CancellationToken, Task>> _stateHandlers;

    public DriverAgent(string username, string password) : this(username, password, new Options()) { }
    public DriverAgent(string username, string password, Options options)
    {
        _options = options;
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
            { TripState.Accepted, async (t, ct) => await HandleAcceptedAsync(t, ct).ConfigureAwait(false) },
            { TripState.DriverArrived, async (t, ct) => await HandleDriverArrivedAsync(t, ct).ConfigureAwait(false) },
            { TripState.AtStop1, async (_, ct) => await HandleAtStopsAsync(ct).ConfigureAwait(false) },
            { TripState.AtStop2, async (_, ct) => await HandleAtStopsAsync(ct).ConfigureAwait(false) },
            { TripState.AtStop3, async (_, ct) => await HandleAtStopsAsync(ct).ConfigureAwait(false) },
            { TripState.AtStop4, async (_, ct) => await HandleAtStopsAsync(ct).ConfigureAwait(false) },
            { TripState.AtStop5, async (_, ct) => await HandleAtStopsAsync(ct).ConfigureAwait(false) },
            { TripState.AfterStop1, async (t, ct) => await HandleMoveAndMaybeStopAsync(t, 2, 3, ct).ConfigureAwait(false) },
            { TripState.AfterStop2, async (t, ct) => await HandleMoveAndMaybeStopAsync(t, 3, 4, ct).ConfigureAwait(false) },
            { TripState.AfterStop3, async (t, ct) => await HandleMoveAndMaybeStopAsync(t, 4, 5, ct).ConfigureAwait(false) },
            { TripState.AfterStop4, async (t, ct) => await HandleMoveAndMaybeStopAsync(t, 5, 6, ct).ConfigureAwait(false) },
            { TripState.AfterStop5, async (t, ct) => { await SafeMoveToIndexAsync(t, 6, ct).ConfigureAwait(false); if (!ct.IsCancellationRequested) await _tripManager.SendArrivedAtDestinationAsync(ct).ConfigureAwait(false); } },
            { TripState.Arrived, async (t, ct) => await HandleArrivedAsync(t, ct).ConfigureAwait(false) }
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
        _tripManager.StatusChanged += async (_, e) => await HandleStatusUpdate(e.Trip).ConfigureAwait(false);

        // Start background connection tasks and keep references so they are not unobserved.
        _locationConnectTask = _locationManager.Connect(_runCts.Token, TimeSpan.FromSeconds(2))
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Console.WriteLine("Location connect failed: " + t.Exception?.GetBaseException().Message);
                }
            }, TaskContinuationOptions.ExecuteSynchronously);

        _tripConnectTask = _tripManager.Connect(_runCts.Token)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Console.WriteLine("Trip connect failed: " + t.Exception?.GetBaseException().Message);
                }
            }, TaskContinuationOptions.ExecuteSynchronously);

        _punchInTask = _tripManager.SendPunchInAsync(_runCts.Token)
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
                    await _wanderCts.CancelAsync().ConfigureAwait(false);
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

            await _tripManager.SendTripReceivedAsync().ConfigureAwait(false);
            await Task.Delay(2000).ConfigureAwait(false);
            await _tripManager.SendAcceptTripAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
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
            if (_wanderCts is null && !ct.IsCancellationRequested)
            {
                _wanderCts = new CancellationTokenSource();
                _ = WanderLoop(_wanderCts.Token);
            }
            return;
        }

        // Delegate the detailed status processing to keep this method simple.
        await ProcessTripStatusAsync(trip, ct).ConfigureAwait(false);
    }

    private async Task ProcessTripStatusAsync(Trip trip, CancellationToken ct)
    {
        try
        {
            if (_stateHandlers.TryGetValue(trip.Status, out var handler))
            {
                await handler(trip, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Movement was cancelled — expected when trip changes or Stop() called.
        }
        catch (Exception ex)
        {
            Console.WriteLine("ProcessTripStatusAsync error: " + ex.Message);
        }
    }

    // Extracted helpers
    private Task SafeMoveToIndexAsync(Trip trip, int index, CancellationToken ct)
    {
        if (index < 0 || trip.Stops.Count <= index)
            return Task.CompletedTask;
        var loc = trip.Stops[index].Place?.Location;
        if (loc is null) return Task.CompletedTask;
        return MoveTo(loc, ct);
    }

    private Task SendArriveOrStopAsync(Trip trip, int minStopsForStop, CancellationToken ct)
    {
        return (trip.Stops.Count > minStopsForStop)
            ? _tripManager.SendArrivedAtStopAsync(ct)
            : _tripManager.SendArrivedAtDestinationAsync(ct);
    }

    private async Task HandleMoveAndMaybeStopAsync(Trip trip, int moveIndex, int minStopsForStop, CancellationToken ct)
    {
        await SafeMoveToIndexAsync(trip, moveIndex, ct).ConfigureAwait(false);
        if (!ct.IsCancellationRequested)
            await SendArriveOrStopAsync(trip, minStopsForStop, ct).ConfigureAwait(false);
    }

    private async Task HandleAcceptedAsync(Trip trip, CancellationToken ct)
    {
        await SafeMoveToIndexAsync(trip, 0, ct).ConfigureAwait(false);
        if (!ct.IsCancellationRequested)
            await _tripManager.SendArrivedAtPickupAsync(ct).ConfigureAwait(false);
    }

    private async Task HandleDriverArrivedAsync(Trip trip, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(_options.ArrivalWaitTimeSeconds), ct).ConfigureAwait(false);
        if (!ct.IsCancellationRequested)
            await SafeMoveToIndexAsync(trip, 1, ct).ConfigureAwait(false);
        if (!ct.IsCancellationRequested)
            await SendArriveOrStopAsync(trip, 2, ct).ConfigureAwait(false);
    }

    private async Task HandleAtStopsAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(_options.StopWaitTimeSeconds), ct).ConfigureAwait(false);
        if (!ct.IsCancellationRequested)
            await _tripManager.SendContinueTripAsync(ct).ConfigureAwait(false);
    }

    private async Task HandleArrivedAsync(Trip trip, CancellationToken ct)
    {
        if (!ct.IsCancellationRequested)
            await Task.Delay(TimeSpan.FromSeconds(_options.PaymentWaitTimeSeconds), ct).ConfigureAwait(false);
        if (!ct.IsCancellationRequested)
            await _tripManager.SendCashPaymentMadeAsync((int)trip.ActualCost, ct).ConfigureAwait(false);
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

        // Observe background task exceptions to avoid unobserved task warnings.
        _locationConnectTask?.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        _tripConnectTask?.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        _punchInTask?.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
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
                await MoveTo(destLocation, ct).ConfigureAwait(false);
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
                await StepTowards(destLocation, _options.Speed, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // expected
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

        await NotifyLocationChanged().ConfigureAwait(false);

        // Simulate the passage of 5 seconds for this step
        await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
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

#pragma warning restore IDE0005 // restore analyzer
