using Channel = System.Threading.Channels.Channel;
using System.Threading.Channels;
using Tut.Common.GServices;
using Tut.Common.Managers;
using Tut.Common.Models;

namespace TutBackend.Tests;

public class DriverLocationManagerTests
{
    private class RequestObservingFake : IGDriverLocationService
    {
        public Channel<GLocation> ReceivedLocations { get; } = Channel.CreateUnbounded<GLocation>();

        public Task RegisterLocation(IAsyncEnumerable<GLocation> locations, global::ProtoBuf.Grpc.CallContext context = default)
        {
            var t = Task.Run(async () =>
            {
                try
                {
                    await foreach (var loc in locations)
                    {
                        await ReceivedLocations.Writer.WriteAsync(loc);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    // ignore
                }
                finally
                {
                    ReceivedLocations.Writer.TryComplete();
                }
            });

            return t;
        }

        public Task<List<DriverLocation>> GetDriverLocations()
        {
            return Task.FromResult(new List<DriverLocation>());
        }

        public Task<List<DriverLocation>> GetLocationHistoryForDriver(GIdRequest request)
        {
            return Task.FromResult(new List<DriverLocation>());
        }
    }

    [Fact]
    public async Task DoesNotSendBeforeRegisterLocation()
    {
        var fake = new RequestObservingFake();
        var mgr = new DriverLocationManagerService("someToken", fake);

        using var cts = new CancellationTokenSource();
        await mgr.Connect(cts.Token, TimeSpan.FromMilliseconds(100));

        // wait a few intervals
        await Task.Delay(350);

        // There should be no locations received because RegisterLocation was never called
        bool any = fake.ReceivedLocations.Reader.TryRead(out var _);
        Assert.False(any, "Manager sent a location before RegisterLocation was called");

        await mgr.Disconnect();
    }

    [Fact]
    public async Task SendsLocationEveryInterval()
    {
        var fake = new RequestObservingFake();
        var mgr = new DriverLocationManagerService("SomeToken", fake);

        using var cts = new CancellationTokenSource();
        await mgr.Connect(cts.Token, TimeSpan.FromMilliseconds(100));

        var loc = new GLocation { Latitude = 1.23, Longitude = 4.56, Timestamp = DateTime.UtcNow };
        mgr.RegisterLocation(loc);

        // wait for a few intervals to pass
        await Task.Delay(5000);

        var received = new List<GLocation>();
        while (fake.ReceivedLocations.Reader.TryRead(out var r))
        {
            received.Add(r);
        }

        Assert.True(received.Count >= 2, $"Expected at least 2 sends, got {received.Count}");
        Assert.Equal(1.23, received[0].Latitude);
        Assert.Equal(4.56, received[0].Longitude);

        await mgr.Disconnect();
    }
}
