using Tut.Common.Models;
using Tut.Common.GServices;
using Tut.Common.Managers;
#pragma warning restore CS8019

namespace Tut.Agents;

public class UserAgent
{
    private readonly string _username;
    private readonly Options _options;
    private CancellationTokenSource _runCts = new();
    private readonly UserTripManager _tripManager;

    public UserAgent(string username) : this(username, new Options()) { }
    public UserAgent(string username, Options options)
    {
        _username = username;
        _options = options;

        // Create standard channel factory and pass token to UserTripManager so it can send Authorization metadata
        GrpcChannelFactory factory = new GrpcChannelFactory("http://localhost:5040");
        _tripManager = new UserTripManager(username, factory);
    }

    public void Start()
    {
        _runCts = new CancellationTokenSource();
        var token = _runCts.Token;

        _tripManager.ErrorReceived += (_, e) => Console.WriteLine($"UA> Error: {e.ErrorText}");
        _tripManager.NotificationReceived += (_, e) => Console.WriteLine($"UA> Notification: {e.NotificationText}");
        _tripManager.DriverLocationsReceived += (_, e) => Console.WriteLine($"UA> Driver locations: {e.Locations.Count}");
        _tripManager.ConnectionStateChanged += (_, e) => Console.WriteLine($"UA> Connection state: {e.NewState}");
        _tripManager.StatusChanged += (_, e) => HandleStatusChanged(e.Trip);
        _tripManager.InquireResultReceived += async (_, e) => await HandleInquireResultReceived(e.Trip);

        // Connect and start loop
        _ = _tripManager.Connect(token);
        _ = RunLoopAsync(token);
    }

    private void HandleStatusChanged(Trip? trip)
    {
        if (trip is null)
        {
            Console.WriteLine("UA> No active trip");
            return;
        }
        Console.WriteLine($"UA> Status changed: TripId={trip.Id} State={trip.Status} Driver={trip.Driver?.Id} / {trip.Driver?.State}");
    }

    private async Task HandleInquireResultReceived(Trip? trip)
    {
        if (trip is null)
        {
            Console.WriteLine("UA> InquireResult with null trip!");
            return;
        }
        await _tripManager.SendRequestTripAsync(trip);
    }

    public void Stop()
    {
        _ = _tripManager.Disconnect();
        _runCts.Cancel();
        _runCts.Dispose();
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
        var rng = Random.Shared;
        while (!token.IsCancellationRequested)
        {
            int delaySec = rng.Next(1, 31); // 1-30 seconds
            Console.WriteLine($"UA> Waiting {delaySec}s before requesting a trip...");
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySec), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (token.IsCancellationRequested) break;

            // Build a trip with pickup, 0-2 stops, and destination
            Trip trip = BuildRandomTrip();
            Console.WriteLine($"UA> Inquiring trip with {trip.Stops.Count} stops (including pickup and dropoff)");
            try
            {
                await _tripManager.SendInquireTripAsync(trip, token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UA> Failed to send InquireTrip: {ex.Message}");
            }

            // Event-based waiter: wait until StatusChanged indicates this trip ended/canceled
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            void Handler(object? s, StatusUpdateEventArgs e)
            {
                try
                {
                    if (e.Trip is null)
                    {
                        tcs.TrySetResult(true);
                        return;
                    }
                    if (e.Trip.Id != trip.Id) return;
                    if (e.Trip.Status == TripState.Ended || e.Trip.Status == TripState.Canceled)
                    {
                        tcs.TrySetResult(true);
                    }
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }

            _tripManager.StatusChanged += Handler;

            using var ctr = CancellationTokenSource.CreateLinkedTokenSource(token);
            var waitTask = tcs.Task;
            try
            {
                await waitTask.WaitAsync(ctr.Token);
                Console.WriteLine("After waitTask");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("... Cancelled");
                // cancel waiting
            }
            finally
            {
                _tripManager.StatusChanged -= Handler;
            }

            // loop continues and will wait 0-30 seconds then request again
        }
    }

    private Trip BuildRandomTrip()
    {
        var rng = Random.Shared;
        // Create pickup
        GLocation loc = RandomLocationInBounds(_options.AreaBottomLeft, _options.AreaTopRight);
        var pickup = new Place
        {
            PlaceType = PlaceType.Stop,
            Name = "Pickup",
            Latitude = loc.Latitude,
            Longitude = loc.Longitude
        };

        int extraStops = rng.Next(0, _options.MaxNumberOfStops - 1); // 0,1,2 intermediate stops
        var stops = new List<Place> { pickup };

        GLocation tripBottomLeft = new GLocation
        {
            Latitude = Math.Max(_options.AreaBottomLeft.Latitude, pickup.Latitude - 0.02),
            Longitude = Math.Max(_options.AreaBottomLeft.Longitude, pickup.Longitude - 0.03)
        };
        GLocation tripTopRight = new GLocation
        {
            Latitude = Math.Min(_options.AreaTopRight.Latitude, pickup.Latitude + 0.02),
            Longitude = Math.Min(_options.AreaTopRight.Longitude, pickup.Longitude + 0.03)
        };
        
        for (int i = 0; i < extraStops; i++)
        {
            loc = RandomLocationInBounds(tripBottomLeft, tripTopRight);
            stops.Add(new Place
            {
                PlaceType = PlaceType.Stop,
                Name = $"Stop{i + 1}",
                Latitude = loc.Latitude,
                Longitude = loc.Longitude,
                Order = i
            });
        }

        loc = RandomLocationInBounds(tripBottomLeft, tripTopRight);
        var dropoff = new Place
        {
            PlaceType = PlaceType.Stop,
            Name = "Dropoff",
            Latitude = loc.Latitude,
            Longitude = loc.Longitude,
        };

        stops.Add(dropoff);

        var trip = new Trip
        {
            User = new User { Id = 1, Mobile = _username, FirstName = "Ualed", LastName = "Uawas" },
            Stops = stops,
            Status = TripState.Requested
        };
        trip.SetRoute(null);
        return trip;
    }

    private GLocation RandomLocationInBounds(GLocation bottomLeft, GLocation topRight)
    {
        double minLat = Math.Min(bottomLeft.Latitude, topRight.Latitude);
        double maxLat = Math.Max(bottomLeft.Latitude, topRight.Latitude);
        double minLon = Math.Min(bottomLeft.Longitude, topRight.Longitude);
        double maxLon = Math.Max(bottomLeft.Longitude, topRight.Longitude);
        var rng = Random.Shared;
        return new GLocation
        {
            Latitude = rng.NextDouble() * (maxLat - minLat) + minLat,
            Longitude = rng.NextDouble() * (maxLon - minLon) + minLon,
            Altitude = 0,
            Course = 0,
            Speed = 0,
            Timestamp = DateTime.UtcNow
        };
    }

    public class Options
    {
        public readonly GLocation AreaBottomLeft = new GLocation { Latitude = 30, Longitude = 31.00 };
        public readonly GLocation AreaTopRight = new GLocation { Latitude = 30.30, Longitude = 31.70 };
        public readonly int MaxNumberOfStops = 3;  // including pickup and dropOff.
    }
}
