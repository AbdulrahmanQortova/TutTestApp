using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Grpc.Core;
using Grpc.Net.Client;
using System.Threading.Channels;
using Channel = System.Threading.Channels.Channel;
using Tut.Common.GServices;
using Tut.Common.Managers;
using Tut.Common.Models;
using Xunit;

namespace TutBackend.Tests;

public class UserTripManagerTests
{
    private class FakeUserTripService : IGUserTripService
    {
        private int _connectCalls = 0;
        private readonly bool _throwOnFirstConnect;
        private readonly int _throwAfterResponses; // if > 0, throw after yielding this many responses
        private readonly StatusCode _throwStatusCode;
        public Channel<UserTripPacket> ResponseChannel { get; } = Channel.CreateUnbounded<UserTripPacket>();

        public FakeUserTripService(bool throwOnFirstConnect = false, int throwAfterResponses = 0, StatusCode throwStatusCode = StatusCode.Unavailable)
        {
            _throwOnFirstConnect = throwOnFirstConnect;
            _throwAfterResponses = throwAfterResponses;
            _throwStatusCode = throwStatusCode;
        }

        public IAsyncEnumerable<UserTripPacket> Connect(IAsyncEnumerable<UserTripPacket> requestPackets, global::ProtoBuf.Grpc.CallContext context = default)
        {
            int callNo = Interlocked.Increment(ref _connectCalls);
            if (callNo == 1 && _throwOnFirstConnect)
            {
                // Simulate transient server unavailability by throwing an RPC exception on first attempt
                throw new RpcException(new Status(StatusCode.Unavailable, "simulated"));
            }

            return ReadResponses();

            async IAsyncEnumerable<UserTripPacket> ReadResponses()
            {
                int yielded = 0;
                await foreach (var p in ResponseChannel.Reader.ReadAllAsync())
                {
                    // Yield the packet
                    yield return p;

                    yielded++;

                    // If configured to throw after N yielded responses, throw now to simulate mid-stream failure
                    if (_throwAfterResponses > 0 && yielded >= _throwAfterResponses)
                    {
                        throw new RpcException(new Status(_throwStatusCode, "simulated mid-stream failure"));
                    }
                }
            }
        }

        public void ProvideFeedback(Feedback feedback)
        {
            // no-op for tests
        }

        public Task<UserTripPacket> GetState()
        {
            return Task.FromResult(new UserTripPacket());
        }
    }

    private static UserTripManager CreateManagerWithFake(IGUserTripService fake)
    {
        // Create a simple channel factory that returns a harmless channel; we'll replace the service instance via reflection
        var factory = new TestGrpcChannelFactory();
        var mgr = new UserTripManager("test-token", factory);
        var fi = typeof(UserTripManager).GetField("_userTrip_service", BindingFlags.Instance | BindingFlags.NonPublic);
        if (fi is null) fi = typeof(UserTripManager).GetField("_userTripService", BindingFlags.Instance | BindingFlags.NonPublic)!;
        fi.SetValue(mgr, fake);
        return mgr;
    }

    private sealed class TestGrpcChannelFactory : IGrpcChannelFactory
    {
        private readonly GrpcChannel _channel = GrpcChannel.ForAddress("http://localhost");
        public GrpcChannel GetChannel() => _channel;
        public GrpcChannel GetChannel(string address) => GrpcChannel.ForAddress(address);
        public GrpcChannel GetNewChannel() => GrpcChannel.ForAddress("http://localhost");
        public GrpcChannel GetNewChannel(string address) => GrpcChannel.ForAddress(address);
    }

    private class RequestObservingFake : IGUserTripService
    {
        public Channel<UserTripPacket> ResponseChannel { get; } = Channel.CreateUnbounded<UserTripPacket>();
        public Channel<UserTripPacket> ReceivedRequests { get; } = Channel.CreateUnbounded<UserTripPacket>();

        public IAsyncEnumerable<UserTripPacket> Connect(IAsyncEnumerable<UserTripPacket> requestPackets, global::ProtoBuf.Grpc.CallContext context = default)
        {
            // start background reader of requests so SendAsync can be observed
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var req in requestPackets)
                    {
                        await ReceivedRequests.Writer.WriteAsync(req);
                    }
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    ReceivedRequests.Writer.TryComplete();
                }
            });

            return ResponseChannel.Reader.ReadAllAsync();
        }

        public void ProvideFeedback(Feedback feedback)
        {
        }

        public Task<UserTripPacket> GetState() => Task.FromResult(new UserTripPacket());
    }

    [Fact]
    public async Task Connect_HappyPath_RaisesEventsAndSetsStateAndCurrentTrip()
    {
        var fake = new FakeUserTripService();
        var mgr = CreateManagerWithFake(fake);

        var statusTcs = new TaskCompletionSource<Trip?>();
        var notifTcs = new TaskCompletionSource<string?>();
        var locationsTcs = new TaskCompletionSource<List<GLocation>?>();
        var stateChanges = new List<ConnectionState>();

        mgr.StatusChanged += (_, e) => statusTcs.TrySetResult(e.Trip);
        mgr.NotificationReceived += (_, e) => notifTcs.TrySetResult(e.NotificationText);
        mgr.DriverLocationsReceived += (_, e) => locationsTcs.TrySetResult(e.Locations);
        mgr.ConnectionStateChanged += (_, e) => stateChanges.Add(e.NewState);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await mgr.Connect(cts.Token);

        // Wait for manager to enter Connecting then Connected
        await Task.Delay(50);
        Assert.Contains(ConnectionState.Connecting, stateChanges);
        Assert.Contains(ConnectionState.Connected, stateChanges);

        // Send a status update with required User set
        var trip = new Trip { Id = 123, User = new User { Id = 1, FirstName = "T", LastName = "U" } };
        await fake.ResponseChannel.Writer.WriteAsync(UserTripPacket.StatusUpdate(trip));

        // Send a notification
        await fake.ResponseChannel.Writer.WriteAsync(new UserTripPacket { Type = UserTripPacketType.Notification, NotificationText = "hello" });

        // Send driver locations
        var locs = new List<GLocation> { new GLocation { Latitude = 1, Longitude = 2 } };
        var locPacket = new UserTripPacket { Type = UserTripPacketType.DriverLocationUpdate, DriverLocations = locs };
        await fake.ResponseChannel.Writer.WriteAsync(locPacket);

        // Close the server stream to let manager finish
        fake.ResponseChannel.Writer.Complete();

        // Await events
        var receivedTrip = await statusTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var receivedNotif = await notifTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var receivedLocs = await locationsTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.NotNull(receivedTrip);
        Assert.Equal(123, receivedTrip.Id);
        Assert.Equal("hello", receivedNotif);
        Assert.Single(receivedLocs!);

        // CurrentTrip should be set
        Assert.NotNull(mgr.CurrentTrip);
        Assert.Equal(123, mgr.CurrentTrip!.Id);

        // Final state should eventually be Disconnected after the server closed the stream
        await Task.Delay(50);
        Assert.Equal(ConnectionState.Disconnected, mgr.CurrentState);
    }

    [Fact]
    public async Task Connect_TransientError_ReconnectsAndRecovers()
    {
        var fake = new FakeUserTripService(throwOnFirstConnect: true);
        var mgr = CreateManagerWithFake(fake);

        var statusTcs = new TaskCompletionSource<Trip?>();
        var stateEvents = new List<ConnectionState>();

        mgr.StatusChanged += (_, e) => statusTcs.TrySetResult(e.Trip);
        mgr.ConnectionStateChanged += (_, e) => stateEvents.Add(e.NewState);
        mgr.ErrorReceived += (_, _) => { /* swallow for test */ };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await mgr.Connect(cts.Token);

        // Wait a small time for reconnect logic to kick in and attempt second connect
        await Task.Delay(700);

        // At this point the fake should have been called again and now will accept responses
        var trip = new Trip { Id = 999, User = new User { Id = 2, FirstName = "X", LastName = "Y" } };
        await fake.ResponseChannel.Writer.WriteAsync(UserTripPacket.StatusUpdate(trip));
        fake.ResponseChannel.Writer.Complete();

        var receivedTrip = await statusTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.NotNull(receivedTrip);
        Assert.Equal(999, receivedTrip.Id);

        // Verify we saw Reconnecting and Connected in stateEvents
        Assert.Contains(ConnectionState.Reconnecting, stateEvents);
        Assert.Contains(ConnectionState.Connected, stateEvents);
    }

    [Fact]
    public async Task Connect_MidStreamTransientError_ReconnectsAndRecovers()
    {
        // Configure fake to throw a transient (Unavailable) RpcException after yielding 1 response
        var fake = new FakeUserTripService(throwOnFirstConnect: false, throwAfterResponses: 1, throwStatusCode: StatusCode.Unavailable);
        var mgr = CreateManagerWithFake(fake);

        var firstStatusTcs = new TaskCompletionSource<Trip?>();
        var recoveredStatusTcs = new TaskCompletionSource<Trip?>();
        var stateEvents = new List<ConnectionState>();

        int receivedCount = 0;
        mgr.StatusChanged += (_, e) =>
        {
            receivedCount++;
            if (receivedCount == 1) firstStatusTcs.TrySetResult(e.Trip);
            if (receivedCount == 2) recoveredStatusTcs.TrySetResult(e.Trip);
        };

        mgr.ConnectionStateChanged += (_, e) => stateEvents.Add(e.NewState);
        mgr.ErrorReceived += (_, _) => { /* swallow for test */ };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await mgr.Connect(cts.Token);

        // Send first status update (will be yielded, then the fake will throw)
        var trip1 = new Trip { Id = 200, User = new User { Id = 10, FirstName = "A", LastName = "B" } };
        await fake.ResponseChannel.Writer.WriteAsync(UserTripPacket.StatusUpdate(trip1));

        // Wait for the first status to be observed
        var received1 = await firstStatusTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.NotNull(received1);
        Assert.Equal(200, received1.Id);

        // Allow time for the mid-stream exception to occur and reconnect to be attempted
        await Task.Delay(600);

        // On the reconnected call, send a second status update and complete
        var trip2 = new Trip { Id = 201, User = new User { Id = 11, FirstName = "C", LastName = "D" } };
        await fake.ResponseChannel.Writer.WriteAsync(UserTripPacket.StatusUpdate(trip2));
        fake.ResponseChannel.Writer.Complete();

        var received2 = await recoveredStatusTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.NotNull(received2);
        Assert.Equal(201, received2.Id);

        // Ensure we saw reconnecting and connected states during the recovery
        Assert.Contains(ConnectionState.Reconnecting, stateEvents);
        Assert.Contains(ConnectionState.Connected, stateEvents);
    }

    [Fact]
    public async Task Connect_MidStreamNonTransientError_StopsAndEmitsError()
    {
        // Configure fake to throw a non-transient RpcException (InvalidArgument) after yielding 1 response
        var fake = new FakeUserTripService(throwOnFirstConnect: false, throwAfterResponses: 1, throwStatusCode: StatusCode.InvalidArgument);
        var mgr = CreateManagerWithFake(fake);

        var statusTcs = new TaskCompletionSource<Trip?>();
        var errorTcs = new TaskCompletionSource<string?>();
        var stateEvents = new List<ConnectionState>();

        mgr.StatusChanged += (_, e) => statusTcs.TrySetResult(e.Trip);
        mgr.ErrorReceived += (_, e) => errorTcs.TrySetResult(e.ErrorText);
        mgr.ConnectionStateChanged += (_, e) => stateEvents.Add(e.NewState);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await mgr.Connect(cts.Token);

        // Send first status update (will be yielded, then the fake will throw non-transient)
        var trip1 = new Trip { Id = 300, User = new User { Id = 20, FirstName = "E", LastName = "F" } };
        await fake.ResponseChannel.Writer.WriteAsync(UserTripPacket.StatusUpdate(trip1));

        // Wait for the status to be observed
        var received = await statusTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.NotNull(received);
        Assert.Equal(300, received.Id);

        // Allow time for the non-transient exception to be propagated
        var err = await errorTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.NotNull(err);
        Assert.Contains("RPC error", err);

        // Manager should end up Disconnected
        await Task.Delay(50);
        Assert.Equal(ConnectionState.Disconnected, mgr.CurrentState);

        // Ensure we did not see Reconnecting (since non-transient error should stop retries)
        Assert.DoesNotContain(ConnectionState.Reconnecting, stateEvents);
    }

    [Fact]
    public async Task SendAsync_NotConnected_ThrowsInvalidOperationException()
    {
        var fake = new RequestObservingFake();
        var mgr = CreateManagerWithFake(fake);

        var pkt = new UserTripPacket { Type = UserTripPacketType.Notification, NotificationText = "x" };
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await mgr.SendAsync(pkt));
    }

    [Fact]
    public async Task SendAsync_SendsPacketsToServerStream()
    {
        var fake = new RequestObservingFake();
        var mgr = CreateManagerWithFake(fake);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await mgr.Connect(cts.Token);

        var pkt = new UserTripPacket { Type = UserTripPacketType.Notification, NotificationText = "from-client" };
        await mgr.SendAsync(pkt);

        // Read the request from the fake's received requests channel
        var readCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var received = await fake.ReceivedRequests.Reader.ReadAsync(readCts.Token);
        Assert.Equal("from-client", received.NotificationText);

        // Complete the response stream so manager can finish cleanly
        fake.ResponseChannel.Writer.Complete();

        // Allow time for disconnection
        await Task.Delay(50);
        Assert.Equal(ConnectionState.Disconnected, mgr.CurrentState);
    }

    [Fact]
    public async Task Connect_CalledTwice_IsNoOp()
    {
        var fake = new FakeUserTripService();
        var mgr = CreateManagerWithFake(fake);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await mgr.Connect(cts.Token);

        // call connect again; should return quickly and not throw
        await mgr.Connect(cts.Token);

        // manager should still be connected (or at least not have transitioned to Disconnected)
        await Task.Delay(50);
        Assert.Contains(ConnectionState.Connected, new[] { mgr.CurrentState });

        // finish
        fake.ResponseChannel.Writer.Complete();
        await Task.Delay(50);
    }
}
