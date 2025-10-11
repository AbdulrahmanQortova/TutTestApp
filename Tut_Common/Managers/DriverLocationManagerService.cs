using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;
using System.Threading.Channels;
using Tut.Common.GServices;
using Tut.Common.Models;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Tut.Common.Managers;

public class DriverLocationManagerService
{
    private readonly IGDriverLocationService _driverLocationService;
    private Channel<GLocation>? _requestChannel;
    private CancellationTokenSource? _cts;
    private Task? _registerTask;
    private Task? _sendLoopTask;

    private readonly Lock _stateLock = new();
    private GLocation? _currentLocation;

    private CallOptions _callOptions;

    public GLocation? CurrentLocation
    {
        get { lock (_stateLock) { return _currentLocation; } }
        private set { lock (_stateLock) { _currentLocation = value; } }
    }

    public event EventHandler<ErrorReceivedEventArgs>? ErrorReceived;

    public DriverLocationManagerService(IGrpcChannelFactory channelFactory)
    {
        GrpcChannel grpcChannel = channelFactory.GetChannel();
        _driverLocationService = grpcChannel.CreateGrpcService<IGDriverLocationService>();
        Metadata metadata = [];
        _callOptions = new CallOptions(metadata);
    }

    // Test-friendly constructor: directly inject the gRPC service implementation
    public DriverLocationManagerService(IGDriverLocationService driverLocationService)
    {
        _driverLocationService = driverLocationService ?? throw new ArgumentNullException(nameof(driverLocationService));
        Metadata metadata = [];
        _callOptions = new CallOptions(metadata);
    }

    public void SetAccessToken(string accessToken)
    {
        Metadata metadata = [];
        metadata.Add("Authorization", $"Bearer {accessToken}");
        _callOptions = new CallOptions(metadata);
    }
    
    
    /// <summary>
    /// Register the latest location locally. The background sender will emit the latest
    /// non-null location to the server every 5 seconds by default. For tests, pass a smaller interval to Connect.
    /// </summary>
    public void RegisterLocation(GLocation location)
    {
        CurrentLocation = location;
    }

    // sendInterval: optional override for the periodic send delay (default 5 seconds)
    public async Task Connect(CancellationToken cancellationToken, TimeSpan? sendInterval = null)
    {
        if (_requestChannel is not null)
            return; // already connected

        _requestChannel = Channel.CreateBounded<GLocation>(new BoundedChannelOptions(20)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancellationToken linkedToken = _cts.Token;

        TimeSpan effectiveInterval = sendInterval ?? TimeSpan.FromSeconds(5);

        // Task that calls the server streaming RPC once and keeps it alive, with retry on transient errors
        _registerTask = Task.Run(async () =>
        {
            int attempt = 0;
            try
            {
                while (!linkedToken.IsCancellationRequested)
                {
                    attempt++;

                    try
                    {
                        var requestStream = _requestChannel!.Reader.ReadAllAsync(linkedToken);

                        attempt = 0;

                        // This is a client-streaming call; it will complete when the server closes or on error
                        await _driverLocationService.RegisterLocation(requestStream, new CallContext(_callOptions));

                        // server closed gracefully - exit loop
                        break;
                    }
                    catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (RpcException rex) when (IsTransient(rex.StatusCode))
                    {
                        ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs { ErrorText = $"Transient network error: {rex.Message}. Reconnecting..." });

                        int delayMs = ComputeBackoffMs(attempt);
                        try
                        {
                            await Task.Delay(delayMs, linkedToken);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                    catch (RpcException rex)
                    {
                        ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs { ErrorText = $"RPC error: {rex.Message}" });
                        break;
                    }
                    catch (Exception ex)
                    {
                        ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs { ErrorText = ex.Message });
                        break;
                    }
                }
            }
            finally
            {
                try
                {
                    _requestChannel?.Writer.TryComplete();
                }
                catch (Exception ex)
                {
                    ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs { ErrorText = ex.Message });
                }
            }
        }, CancellationToken.None);

        // Task that periodically (every 5s) emits the last-known location to the request channel
        _sendLoopTask = Task.Run(async () =>
        {
            while (!linkedToken.IsCancellationRequested)
            {
                GLocation? loc;
                lock (_stateLock)
                {
                    loc = _currentLocation;
                }

                if (loc is not null)
                {
                    try
                    {
                        await _requestChannel!.Writer.WriteAsync(loc, linkedToken);
                    }
                    catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs { ErrorText = ex.Message });
                    }
                }

                try
                {
                    await Task.Delay(effectiveInterval, linkedToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, CancellationToken.None);

        await Task.CompletedTask;
    }

    public async Task Disconnect()
    {
        if (_cts is not null)
            await _cts.CancelAsync();

        _requestChannel?.Writer.TryComplete();

        if (_sendLoopTask is not null)
        {
            try
            {
                await _sendLoopTask;
            }
            catch (Exception ex)
            {
                ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs { ErrorText = ex.Message });
            }
        }

        if (_registerTask is not null)
        {
            try
            {
                await _registerTask;
            }
            catch (Exception ex)
            {
                ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs { ErrorText = ex.Message });
            }
        }

        _requestChannel = null;
        _cts?.Dispose();
        _cts = null;
    }

    private static bool IsTransient(StatusCode code)
    {
        return code switch
        {
            StatusCode.Unavailable => true,
            StatusCode.DeadlineExceeded => true,
            StatusCode.ResourceExhausted => true,
            StatusCode.Internal => true,
            StatusCode.Unknown => true,
            _ => false,
        };
    }

    private static int ComputeBackoffMs(int attempt)
    {
        if (attempt <= 0) attempt = 1;
        double baseMs = 500.0 * Math.Pow(2.0, Math.Min(attempt - 1, 4));
        int capMs = 8000;
        int ms = (int)Math.Min(baseMs, capMs);
        double jitter = (Random.Shared.NextDouble() * 0.4) - 0.2;
        ms = (int)Math.Max(100, ms + ms * jitter);
        return ms;
    }

    public void Dispose()
    {
        try
        {
            Disconnect().GetAwaiter().GetResult();
        }
        catch
        {
            // ignore
        }
    }
}
