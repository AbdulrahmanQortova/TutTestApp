using System.Reflection;
using Grpc.Core;
using Grpc.Net.Client;
using System.Threading.Channels;
using Channel = System.Threading.Channels.Channel;
using Tut.Common.GServices;
using Tut.Common.Managers;
using Tut.Common.Models;

namespace TutBackend.Tests;

public class DriverTripManagerTests
{
    private class FakeDriverTripService : IGDriverTripService
    {
        private int _connectCalls;
        private readonly bool _throwOnFirstConnect;
        private readonly int _throwAfterResponses; // if > 0, throw after yielding this many responses
        private readonly StatusCode _throwStatusCode;
        public Channel<DriverTripPacket> ResponseChannel { get; } = Channel.CreateUnbounded<DriverTripPacket>();

        public FakeDriverTripService(bool throwOnFirstConnect = false, int throwAfterResponses = 0, StatusCode throwStatusCode = StatusCode.Unavailable)
        {
            _throwOnFirstConnect = throwOnFirstConnect;
            _throwAfterResponses = throwAfterResponses;
            _throwStatusCode = throwStatusCode;
        }

        public IAsyncEnumerable<DriverTripPacket> Connect(IAsyncEnumerable<DriverTripPacket> requestPackets, global::ProtoBuf.Grpc.CallContext context = default)
        {
            int callNo = Interlocked.Increment(ref _connectCalls);
            if (callNo == 1 && _throwOnFirstConnect)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "simulated"));
            }

            return ReadResponses();

            async IAsyncEnumerable<DriverTripPacket> ReadResponses()
            {
                int yielded = 0;
                await foreach (var p in ResponseChannel.Reader.ReadAllAsync())
                {
                    yield return p;
                    yielded++;
                    if (_throwAfterResponses > 0 && yielded >= _throwAfterResponses)
                    {
                        throw new RpcException(new Status(_throwStatusCode, "simulated mid-stream failure"));
                    }
                }
            }
        }
    }

    private static DriverTripManager CreateManagerWithFake(IGDriverTripService fake)
    {
        var factory = new TestGrpcChannelFactory();
        var mgr = new DriverTripManager(factory);
        var fi = typeof(DriverTripManager).GetField("_driverTripService", BindingFlags.Instance | BindingFlags.NonPublic)!;
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

    private class RequestObservingFake : IGDriverTripService
    {
        public Channel<DriverTripPacket> ResponseChannel { get; } = Channel.CreateUnbounded<DriverTripPacket>();
        public Channel<DriverTripPacket> ReceivedRequests { get; } = Channel.CreateUnbounded<DriverTripPacket>();

        public IAsyncEnumerable<DriverTripPacket> Connect(IAsyncEnumerable<DriverTripPacket> requestPackets, global::ProtoBuf.Grpc.CallContext context = default)
        {
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
    }

    [Fact]
    public async Task Connect_HappyPath_RaisesEventsAndSetsStateAndCurrentTrip()
    {
        var fake = new FakeDriverTripService();
        var mgr = CreateManagerWithFake(fake);

        var statusTcs = new TaskCompletionSource<Trip?>();
        var offerTcs = new TaskCompletionSource<Trip?>();
        var stateChanges = new List<ConnectionState>();

        mgr.StatusChanged += (_, e) => statusTcs.TrySetResult(e.Trip);
        mgr.OfferReceived += (_, e) => offerTcs.TrySetResult(e.Trip);
        mgr.ConnectionStateChanged += (_, e) => stateChanges.Add(e.NewState);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await mgr.Connect(cts.Token);

        // Wait for connecting/connected
        await Task.Delay(50);
        Assert.Contains(ConnectionState.Connecting, stateChanges);
        Assert.Contains(ConnectionState.Connected, stateChanges);

        // Send an offer
        var tripOffer = new Trip { Id = 111, User = new User { Id = 5, FirstName = "O", LastName = "F" } };
        await fake.ResponseChannel.Writer.WriteAsync(DriverTripPacket.StatusUpdate(tripOffer).WithType(DriverTripPacketType.OfferTrip));

        // Send a status update
        var tripStatus = new Trip { Id = 112, User = new User { Id = 6, FirstName = "S", LastName = "T" } };
        await fake.ResponseChannel.Writer.WriteAsync(DriverTripPacket.StatusUpdate(tripStatus));

        fake.ResponseChannel.Writer.Complete();

        var receivedOffer = await offerTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var receivedStatus = await statusTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.NotNull(receivedOffer);
        Assert.Equal(111, receivedOffer.Id);
        Assert.NotNull(receivedStatus);
        Assert.Equal(112, receivedStatus.Id);

        Assert.NotNull(mgr.CurrentTrip);
        Assert.Equal(112, mgr.CurrentTrip!.Id);

        await Task.Delay(50);
        Assert.Equal(ConnectionState.Disconnected, mgr.CurrentState);
    }

    [Fact]
    public async Task Connect_TransientError_ReconnectsAndRecovers()
    {
        var fake = new FakeDriverTripService(throwOnFirstConnect: true);
        var mgr = CreateManagerWithFake(fake);

        var statusTcs = new TaskCompletionSource<Trip?>();
        var stateEvents = new List<ConnectionState>();

        mgr.StatusChanged += (_, e) => statusTcs.TrySetResult(e.Trip);
        mgr.ConnectionStateChanged += (_, e) => stateEvents.Add(e.NewState);
        mgr.ErrorReceived += (_, _) => { };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await mgr.Connect(cts.Token);

        await Task.Delay(700);

        var trip = new Trip { Id = 9999, User = new User { Id = 2, FirstName = "X", LastName = "Y" } };
        await fake.ResponseChannel.Writer.WriteAsync(DriverTripPacket.StatusUpdate(trip));
        fake.ResponseChannel.Writer.Complete();

        var receivedTrip = await statusTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.NotNull(receivedTrip);
        Assert.Equal(9999, receivedTrip.Id);

        Assert.Contains(ConnectionState.Reconnecting, stateEvents);
        Assert.Contains(ConnectionState.Connected, stateEvents);
    }

    [Fact]
    public async Task Connect_MidStreamTransientError_ReconnectsAndRecovers()
    {
        var fake = new FakeDriverTripService(throwOnFirstConnect: false, throwAfterResponses: 1, throwStatusCode: StatusCode.Unavailable);
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
        mgr.ErrorReceived += (_, _) => { };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await mgr.Connect(cts.Token);

        var trip1 = new Trip { Id = 2000, User = new User { Id = 10, FirstName = "A", LastName = "B" } };
        await fake.ResponseChannel.Writer.WriteAsync(DriverTripPacket.StatusUpdate(trip1));

        var received1 = await firstStatusTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.NotNull(received1);
        Assert.Equal(2000, received1.Id);

        await Task.Delay(600);

        var trip2 = new Trip { Id = 2001, User = new User { Id = 11, FirstName = "C", LastName = "D" } };
        await fake.ResponseChannel.Writer.WriteAsync(DriverTripPacket.StatusUpdate(trip2));
        fake.ResponseChannel.Writer.Complete();

        var received2 = await recoveredStatusTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.NotNull(received2);
        Assert.Equal(2001, received2.Id);

        Assert.Contains(ConnectionState.Reconnecting, stateEvents);
        Assert.Contains(ConnectionState.Connected, stateEvents);
    }

    [Fact]
    public async Task Connect_MidStreamNonTransientError_StopsAndEmitsError()
    {
        var fake = new FakeDriverTripService(throwOnFirstConnect: false, throwAfterResponses: 1, throwStatusCode: StatusCode.InvalidArgument);
        var mgr = CreateManagerWithFake(fake);

        var statusTcs = new TaskCompletionSource<Trip?>();
        var errorTcs = new TaskCompletionSource<string?>();
        var stateEvents = new List<ConnectionState>();

        mgr.StatusChanged += (_, e) => statusTcs.TrySetResult(e.Trip);
        mgr.ErrorReceived += (_, e) => errorTcs.TrySetResult(e.ErrorText);
        mgr.ConnectionStateChanged += (_, e) => stateEvents.Add(e.NewState);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await mgr.Connect(cts.Token);

        var trip1 = new Trip { Id = 3000, User = new User { Id = 20, FirstName = "E", LastName = "F" } };
        await fake.ResponseChannel.Writer.WriteAsync(DriverTripPacket.StatusUpdate(trip1));

        var received = await statusTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.NotNull(received);
        Assert.Equal(3000, received.Id);

        var err = await errorTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.NotNull(err);
        Assert.Contains("RPC error", err);

        await Task.Delay(50);
        Assert.Equal(ConnectionState.Disconnected, mgr.CurrentState);
        Assert.DoesNotContain(ConnectionState.Reconnecting, stateEvents);
    }

    [Fact]
    public async Task SendAsync_NotConnected_ThrowsInvalidOperationException()
    {
        var fake = new RequestObservingFake();
        var mgr = CreateManagerWithFake(fake);

        var pkt = new DriverTripPacket { Type = DriverTripPacketType.PunchIn };
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await mgr.SendAsync(pkt));
    }

    [Fact]
    public async Task SendAsync_SendsPacketsToServerStream()
    {
        var fake = new RequestObservingFake();
        var mgr = CreateManagerWithFake(fake);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await mgr.Connect(cts.Token);

        var pkt = new DriverTripPacket { Type = DriverTripPacketType.PunchIn };
        await mgr.SendAsync(pkt);

        var readCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var received = await fake.ReceivedRequests.Reader.ReadAsync(readCts.Token);
        Assert.Equal(DriverTripPacketType.PunchIn, received.Type);

        fake.ResponseChannel.Writer.Complete();
        await Task.Delay(50);
        Assert.Equal(ConnectionState.Disconnected, mgr.CurrentState);
    }

    [Fact]
    public async Task Connect_CalledTwice_IsNoOp()
    {
        var fake = new FakeDriverTripService();
        var mgr = CreateManagerWithFake(fake);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await mgr.Connect(cts.Token);

        await mgr.Connect(cts.Token);

        await Task.Delay(50);
        Assert.Contains(ConnectionState.Connected, new[] { mgr.CurrentState });

        fake.ResponseChannel.Writer.Complete();
        await Task.Delay(50);
    }
}

// Helper extension used only in tests to change the type on a packet returned by factory
static class DriverTripPacketExtensions
{
    public static DriverTripPacket WithType(this DriverTripPacket pkt, DriverTripPacketType type)
    {
        pkt.Type = type;
        return pkt;
    }
}
