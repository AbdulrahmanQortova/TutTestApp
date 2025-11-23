using Tut.Common.Managers;
using Tut.Common.Mocks;
using Tut.Common.Models;
using Xunit;

namespace Tut.Common.Tests;

public class MockUserTripManagerTests
{
    // Use fast timing for tests (100x faster than defaults)
    private const int FastConnectionDelayMs = 5;
    private const int FastInquiryDelayMs = 5;
    private const int FastStateTransitionDelayMs = 50;
    private const int FastLongStateTransitionDelayMs = 100;

    private static MockUserTripManager CreateFastMockManager()
    {
        return new MockUserTripManager
        {
            ConnectionDelayMs = FastConnectionDelayMs,
            InquiryDelayMs = FastInquiryDelayMs,
            StateTransitionDelayMs = FastStateTransitionDelayMs,
            LongStateTransitionDelayMs = FastLongStateTransitionDelayMs
        };
    }

    private static Trip CreateSingleStopTrip()
    {
        return new Trip
        {
            Id = 1,
            Stops =
            [
                new Place { Latitude = 40.7128, Longitude = -74.0060, Name = "Pickup", PlaceType = PlaceType.Location },
                new Place { Latitude = 40.7589, Longitude = -73.9851, Name = "Dropoff", PlaceType = PlaceType.Location }
            ]
        };
    }

    private static Trip CreateMultiStopTrip()
    {
        return new Trip
        {
            Id = 2,
            Stops =
            [
                new Place { Latitude = 40.7128, Longitude = -74.0060, Name = "Pickup", PlaceType = PlaceType.Location },
                new Place { Latitude = 40.7589, Longitude = -73.9851, Name = "Stop1", PlaceType = PlaceType.Stop },
                new Place { Latitude = 40.7489, Longitude = -73.9680, Name = "Stop2", PlaceType = PlaceType.Stop },
                new Place { Latitude = 40.7400, Longitude = -73.9500, Name = "Dropoff", PlaceType = PlaceType.Location }
            ]
        };
    }

    [Fact]
    public void InitialState_IsDisconnected()
    {
        var manager = CreateFastMockManager();
        
        Assert.Equal(ConnectionState.Disconnected, manager.CurrentState);
    }

    [Fact]
    public void InitialTrip_IsNull()
    {
        var manager = CreateFastMockManager();
        
        Assert.Null(manager.CurrentTrip);
    }

    [Fact]
    public async Task Connect_ChangesStateToConnected()
    {
        var manager = CreateFastMockManager();
        
        await manager.Connect(CancellationToken.None);
        
        Assert.Equal(ConnectionState.Connected, manager.CurrentState);
    }

    [Fact]
    public async Task Connect_FiresConnectionStateChangedEvent()
    {
        var manager = CreateFastMockManager();
        ConnectionStateChangedEventArgs? capturedArgs = null;
        manager.ConnectionStateChanged += (_, args) => capturedArgs = args;
        
        await manager.Connect(CancellationToken.None);
        
        Assert.NotNull(capturedArgs);
        Assert.Equal(ConnectionState.Disconnected, capturedArgs.OldState);
        Assert.Equal(ConnectionState.Connected, capturedArgs.NewState);
    }

    [Fact]
    public async Task Disconnect_ChangesStateToDisconnected()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        
        await manager.Disconnect();
        
        Assert.Equal(ConnectionState.Disconnected, manager.CurrentState);
    }

    [Fact]
    public async Task Disconnect_FiresConnectionStateChangedEvent()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        
        ConnectionStateChangedEventArgs? capturedArgs = null;
        manager.ConnectionStateChanged += (_, args) => capturedArgs = args;
        
        await manager.Disconnect();
        
        Assert.NotNull(capturedArgs);
        Assert.Equal(ConnectionState.Connected, capturedArgs.OldState);
        Assert.Equal(ConnectionState.Disconnected, capturedArgs.NewState);
    }

    [Fact]
    public async Task SendInquireTripAsync_WhenNotConnected_FiresErrorEvent()
    {
        var manager = CreateFastMockManager();
        var trip = CreateSingleStopTrip();
        ErrorReceivedEventArgs? capturedError = null;
        manager.ErrorReceived += (_, args) => capturedError = args;
        
        await manager.SendInquireTripAsync(trip);
        
        Assert.NotNull(capturedError);
        Assert.Equal("Not connected", capturedError.ErrorText);
    }

    [Fact]
    public async Task SendInquireTripAsync_WhenConnected_SetsCurrentTrip()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        var trip = CreateSingleStopTrip();
        
        await manager.SendInquireTripAsync(trip);
        
        Assert.NotNull(manager.CurrentTrip);
        Assert.Equal(trip.Id, manager.CurrentTrip.Id);
    }

    [Fact]
    public async Task SendInquireTripAsync_SetsEstimatedValues()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        var trip = CreateSingleStopTrip();
        
        await manager.SendInquireTripAsync(trip);
        
        Assert.NotNull(manager.CurrentTrip);
        Assert.True(manager.CurrentTrip.EstimatedArrivalDuration > 0);
        Assert.True(manager.CurrentTrip.EstimatedDistance > 0);
        Assert.True(manager.CurrentTrip.EstimatedTripDuration > 0);
        Assert.True(manager.CurrentTrip.EstimatedCost > 0);
    }

    [Fact]
    public async Task SendInquireTripAsync_FiresInquireResultReceivedEvent()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        var trip = CreateSingleStopTrip();
        InquireResultEventArgs? capturedArgs = null;
        manager.InquireResultReceived += (_, args) => capturedArgs = args;
        
        await manager.SendInquireTripAsync(trip);
        
        Assert.NotNull(capturedArgs);
        Assert.NotNull(capturedArgs.Trip);
        Assert.Equal(trip.Id, capturedArgs.Trip.Id);
    }

    [Fact]
    public async Task SendRequestTripAsync_WhenNotConnected_FiresErrorEvent()
    {
        var manager = CreateFastMockManager();
        var trip = CreateSingleStopTrip();
        ErrorReceivedEventArgs? capturedError = null;
        manager.ErrorReceived += (_, args) => capturedError = args;
        
        await manager.SendRequestTripAsync(trip);
        
        Assert.NotNull(capturedError);
        Assert.Equal("Not connected", capturedError.ErrorText);
    }

    [Fact]
    public async Task SendRequestTripAsync_ProgressesThroughTripStates()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        var trip = CreateSingleStopTrip();
        await manager.SendInquireTripAsync(trip);
        
        var stateChanges = new List<TripState>();
        manager.StatusChanged += (_, args) =>
        {
            if (args.Trip != null)
                stateChanges.Add(args.Trip.Status);
        };
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await manager.SendRequestTripAsync(trip, cts.Token);
        
        // Verify the expected state progression
        Assert.Contains(TripState.Requested, stateChanges);
        Assert.Contains(TripState.Acknowledged, stateChanges);
        Assert.Contains(TripState.Accepted, stateChanges);
        Assert.Contains(TripState.DriverArrived, stateChanges);
        Assert.Contains(TripState.Ongoing, stateChanges);
        Assert.Contains(TripState.Arrived, stateChanges);
        Assert.Contains(TripState.Ended, stateChanges);
    }

    [Fact]
    public async Task SendRequestTripAsync_StatesAreInCorrectOrder()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        var trip = CreateSingleStopTrip();
        await manager.SendInquireTripAsync(trip);
        
        var stateChanges = new List<TripState>();
        manager.StatusChanged += (_, args) =>
        {
            if (args.Trip != null)
                stateChanges.Add(args.Trip.Status);
        };
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await manager.SendRequestTripAsync(trip, cts.Token);
        
        // Verify order
        var expectedOrder = new[]
        {
            TripState.Requested,
            TripState.Acknowledged,
            TripState.Accepted,
            TripState.DriverArrived,
            TripState.Ongoing,
            TripState.Arrived,
            TripState.Ended
        };
        
        Assert.Equal(expectedOrder, stateChanges);
    }

    [Fact]
    public async Task SendRequestTripAsync_AssignsDriverWhenAccepted()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        var trip = CreateSingleStopTrip();
        await manager.SendInquireTripAsync(trip);
        
        Driver? assignedDriver = null;
        manager.StatusChanged += (_, args) =>
        {
            if (args.Trip?.Status == TripState.Accepted && args.Trip.Driver != null)
                assignedDriver = args.Trip.Driver;
        };
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await manager.SendRequestTripAsync(trip, cts.Token);
        
        Assert.NotNull(assignedDriver);
        Assert.NotEmpty(assignedDriver.FirstName);
        Assert.NotEmpty(assignedDriver.LastName);
        Assert.True(assignedDriver.Rating > 0);
    }

    [Fact]
    public async Task SendRequestTripAsync_IncrementsNextStopAtDriverArrived()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        var trip = CreateSingleStopTrip();
        await manager.SendInquireTripAsync(trip);
        
        int? nextStopAfterArrival = null;
        manager.StatusChanged += (_, args) =>
        {
            if (args.Trip?.Status == TripState.DriverArrived)
                nextStopAfterArrival = args.Trip.NextStop;
        };
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await manager.SendRequestTripAsync(trip, cts.Token);
        
        Assert.NotNull(nextStopAfterArrival);
        Assert.Equal(1, nextStopAfterArrival.Value);
    }

    [Fact]
    public async Task SendRequestTripAsync_SetsActualCostWhenArrived()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        var trip = CreateSingleStopTrip();
        await manager.SendInquireTripAsync(trip);
        
        double? actualCost = null;
        manager.StatusChanged += (_, args) =>
        {
            if (args.Trip?.Status == TripState.Arrived)
                actualCost = args.Trip.ActualCost;
        };
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await manager.SendRequestTripAsync(trip, cts.Token);
        
        Assert.NotNull(actualCost);
        Assert.True(actualCost.Value > 0);
    }

    [Fact]
    public async Task SendRequestTripAsync_HandlesMultiStopTrip()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        var trip = CreateMultiStopTrip();
        await manager.SendInquireTripAsync(trip);
        
        var stateChanges = new List<TripState>();
        manager.StatusChanged += (_, args) =>
        {
            if (args.Trip != null)
                stateChanges.Add(args.Trip.Status);
        };
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await manager.SendRequestTripAsync(trip, cts.Token);
        
        // Count AtStop occurrences (should be equal to intermediate stops)
        int atStopCount = stateChanges.Count(s => s == TripState.AtStop);
        int intermediateStops = trip.Stops.Count - 2; // Exclude pickup and dropoff
        
        Assert.Equal(intermediateStops, atStopCount);
    }

    [Fact]
    public async Task SendRequestTripAsync_AlternatesOngoingAndAtStopStates()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        var trip = CreateMultiStopTrip();
        await manager.SendInquireTripAsync(trip);
        
        var stateChanges = new List<TripState>();
        manager.StatusChanged += (_, args) =>
        {
            if (args.Trip != null)
                stateChanges.Add(args.Trip.Status);
        };
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await manager.SendRequestTripAsync(trip, cts.Token);
        
        // Find first Ongoing after DriverArrived
        int firstOngoingIndex = stateChanges.IndexOf(TripState.Ongoing);
        Assert.True(firstOngoingIndex >= 0);
        
        // After first Ongoing, we should have AtStop, Ongoing, AtStop, Ongoing pattern
        var afterFirstOngoing = stateChanges.Skip(firstOngoingIndex + 1).ToList();
        
        // Should start with AtStop
        if (afterFirstOngoing.Count > 0)
        {
            // For multi-stop, we expect AtStop followed by Ongoing
            int firstAtStop = afterFirstOngoing.IndexOf(TripState.AtStop);
            if (firstAtStop >= 0 && firstAtStop + 1 < afterFirstOngoing.Count)
            {
                Assert.Equal(TripState.Ongoing, afterFirstOngoing[firstAtStop + 1]);
            }
        }
    }

    [Fact]
    public async Task SendRequestTripAsync_RespectsEarlyCancellation()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        var trip = CreateSingleStopTrip();
        await manager.SendInquireTripAsync(trip);
        
        var stateChanges = new List<TripState>();
        manager.StatusChanged += (_, args) =>
        {
            if (args.Trip != null)
                stateChanges.Add(args.Trip.Status);
        };
        
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(70));
        await manager.SendRequestTripAsync(trip, cts.Token);
        
        // Should have been cancelled before reaching Ended
        Assert.DoesNotContain(TripState.Ended, stateChanges);
    }

    [Fact]
    public async Task SendRequestTripAsync_InvokesStatusChangedForEachState()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        var trip = CreateSingleStopTrip();
        await manager.SendInquireTripAsync(trip);
        
        int eventCount = 0;
        manager.StatusChanged += (_, _) => eventCount++;
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await manager.SendRequestTripAsync(trip, cts.Token);
        
        Assert.True(eventCount >= 7); // At least 7 state changes for single stop
    }

    [Fact]
    public async Task CurrentTrip_IsThreadSafe()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        var trip = CreateSingleStopTrip();
        await manager.SendInquireTripAsync(trip);
        
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        
        // Start trip simulation
        tasks.Add(Task.Run(async () =>
        {
            try
            {
                await manager.SendRequestTripAsync(trip, cts.Token);
            }
            catch (Exception ex)
            {
                lock (exceptions)
                    exceptions.Add(ex);
            }
        }));
        
        // Concurrently read CurrentTrip many times
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < 10; j++)
                    {
                        _ = manager.CurrentTrip;
                        Task.Delay(1).Wait();
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                        exceptions.Add(ex);
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task CurrentState_IsThreadSafe()
    {
        var manager = CreateFastMockManager();
        
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();
        
        // Concurrently connect/disconnect and read state
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await manager.Connect(CancellationToken.None);
                    await Task.Delay(10);
                    await manager.Disconnect();
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                        exceptions.Add(ex);
                }
            }));
            
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < 20; j++)
                    {
                        _ = manager.CurrentState;
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                        exceptions.Add(ex);
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task ConnectionStateChanged_IsNotFiredWhenStateDoesNotChange()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        
        int eventCount = 0;
        manager.ConnectionStateChanged += (_, _) => eventCount++;
        
        await manager.Connect(CancellationToken.None);
        
        // Should not fire event since already connected
        Assert.Equal(0, eventCount);
    }

    [Fact]
    public async Task StatusChanged_ContainsCorrectTripReference()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        var trip = CreateSingleStopTrip();
        await manager.SendInquireTripAsync(trip);
        
        var capturedTrips = new List<Trip>();
        manager.StatusChanged += (_, args) =>
        {
            if (args.Trip != null)
                capturedTrips.Add(args.Trip);
        };
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await manager.SendRequestTripAsync(trip, cts.Token);
        
        // All captured trips should have the same ID
        Assert.All(capturedTrips, t => Assert.Equal(trip.Id, t.Id));
    }

    [Fact]
    public async Task Connect_WithCancellationToken_CanBeCancelled()
    {
        var manager = CreateFastMockManager();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await manager.Connect(cts.Token));
    }

    [Fact]
    public async Task SendInquireTripAsync_WithCancellationToken_CanBeCancelled()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        var trip = CreateSingleStopTrip();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await manager.SendInquireTripAsync(trip, cts.Token));
    }

    [Fact]
    public async Task SendCancelTripAsync_ThrowsNotImplementedException()
    {
        var manager = CreateFastMockManager();
        
        await Assert.ThrowsAsync<NotImplementedException>(async () =>
            await manager.SendCancelTripAsync());
    }

    [Fact]
    public async Task SendAsync_ThrowsNotImplementedException()
    {
        var manager = CreateFastMockManager();
        var packet = new UserTripPacket { Type = UserTripPacketType.Unspecified };
        
        await Assert.ThrowsAsync<NotImplementedException>(async () =>
            await manager.SendAsync(packet));
    }

    [Fact]
    public async Task TripSimulation_WithNullCurrentTrip_ReturnsImmediately()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        
        // Don't set a trip, just try to request
        var trip = CreateSingleStopTrip();
        // Manually set CurrentTrip to null by disconnecting and reconnecting
        await manager.Disconnect();
        await manager.Connect(CancellationToken.None);
        
        int eventCount = 0;
        manager.StatusChanged += (_, _) => eventCount++;
        
        // This should return immediately since no trip was inquired
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await manager.SendRequestTripAsync(trip, cts.Token);
        
        // Should not fire any status events
        Assert.Equal(0, eventCount);
    }

    [Fact]
    public async Task MultipleConcurrentInquiries_UpdateCurrentTrip()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        
        var trip1 = CreateSingleStopTrip();
        var trip2 = CreateMultiStopTrip();
        
        await manager.SendInquireTripAsync(trip1);
        Assert.Equal(trip1.Id, manager.CurrentTrip?.Id);
        
        await manager.SendInquireTripAsync(trip2);
        Assert.Equal(trip2.Id, manager.CurrentTrip?.Id);
    }

    [Fact]
    public async Task SendRequestTripAsync_StopsIncrementCorrectly()
    {
        var manager = CreateFastMockManager();
        await manager.Connect(CancellationToken.None);
        var trip = CreateMultiStopTrip();
        await manager.SendInquireTripAsync(trip);
        
        var nextStopValues = new List<int>();
        manager.StatusChanged += (_, args) =>
        {
            if (args.Trip != null)
                nextStopValues.Add(args.Trip.NextStop);
        };
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await manager.SendRequestTripAsync(trip, cts.Token);
        
        // NextStop should increment from 0 (initial) to number of stops
        Assert.Contains(0, nextStopValues); // Initial
        Assert.Contains(1, nextStopValues); // After DriverArrived
        Assert.Contains(2, nextStopValues); // After first AtStop
        Assert.Contains(3, nextStopValues); // After second AtStop
    }
}

