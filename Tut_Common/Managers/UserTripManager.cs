using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;
using System.Threading.Channels;
using Tut.Common.GServices;
using Tut.Common.Models;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Tut.Common.Managers;

public class UserTripManager
{
    private readonly IGUserTripService _userTripService;
    private Channel<UserTripPacket>? _requestChannel;
    private CancellationTokenSource? _cts;
    private Task? _receiveLoopTask;

    private readonly Lock _stateLock = new();
    private ConnectionState _connectionState = ConnectionState.Disconnected;
    private Trip? _currentTrip;

    private readonly CallOptions _callOptions; // added: store call options with metadata

    // Public read-only accessors
    public ConnectionState CurrentState
    {
        get
        {
            lock (_stateLock) { return _connectionState; }
        }
    }

    public Trip? CurrentTrip
    {
        get
        {
            lock (_stateLock) { return _currentTrip; }
        }
        private set
        {
            lock (_stateLock) { _currentTrip = value; }
        }
    }

    public event EventHandler<StatusUpdateEventArgs>? StatusChanged;
    public event EventHandler<ErrorReceivedEventArgs>? ErrorReceived; 
    public event EventHandler<NotificationReceivedEventArgs>? NotificationReceived;
    public event EventHandler<DriverLocationsReceivedEventArgs>? DriverLocationsReceived; 
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    public UserTripManager(
        string token,
        IGrpcChannelFactory channelFactory
        )
    {
        GrpcChannel grpcChannel = channelFactory.GetChannel();
        _userTripService = grpcChannel.CreateGrpcService<IGUserTripService>();

        // Populate CallOptions with Authorization header (Bearer {token}) similar to DriverTripManager
        Metadata metadata = [];
        metadata.Add("Authorization", $"Bearer {token}");
        _callOptions = new CallOptions(metadata);
    }


    public async Task Connect(CancellationToken cancellationToken)
    {
        if (_requestChannel is not null)
            return; // already connected

        _requestChannel = Channel.CreateBounded<UserTripPacket>(new BoundedChannelOptions(20)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancellationToken linkedToken = _cts.Token;

        // Start a background task that attempts to (re)connect and consumes server responses.
        // On transient network errors we retry with exponential backoff + jitter.
        _receiveLoopTask = Task.Run(async () =>
        {
            int attempt = 0;
            try
            {
                while (!linkedToken.IsCancellationRequested)
                {
                    attempt++;

                    // Determine the intended connection state for this attempt
                    if (attempt == 1)
                        SetConnectionState(ConnectionState.Connecting);
                    else
                        SetConnectionState(ConnectionState.Reconnecting);

                    try
                    {
                        // Create a fresh request enumerable for each connect attempt
                        var requestStream = _requestChannel!.Reader.ReadAllAsync(linkedToken);
                        // Pass call options via CallContext so Authorization metadata is sent
                        var responseStream = _userTripService.Connect(requestStream, new CallContext(_callOptions));

                        // We successfully established a connection: reset attempt counter
                        attempt = 0;

                        // Mark connected (we established the streaming call)
                        SetConnectionState(ConnectionState.Connected);

                        await foreach (var packet in responseStream.WithCancellation(linkedToken))
                        {
                            switch (packet.Type)
                            {
                                case UserTripPacketType.Error:
                                    ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs { ErrorText = packet.ErrorText });
                                    break;
                                case UserTripPacketType.StatusUpdate:
                                    // update the cached current trip and then notify listeners
                                    CurrentTrip = packet.Trip;
                                    StatusChanged?.Invoke(this, new StatusUpdateEventArgs { Trip = packet.Trip });
                                    break;
                                case UserTripPacketType.Notification:
                                    NotificationReceived?.Invoke(this, new NotificationReceivedEventArgs { NotificationText = packet.NotificationText });
                                    break;
                                case UserTripPacketType.DriverLocationUpdate:
                                    DriverLocationsReceived?.Invoke(this, new DriverLocationsReceivedEventArgs { Locations = packet.DriverLocations });
                                    break;
                            }
                        }

                        // If the server gracefully closed the stream, break the loop and stop reconnecting
                        break;
                    }
                    catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
                    {
                        // cancellation requested -> exit loop
                        break;
                    }
                    catch (RpcException rex) when (IsTransient(rex.StatusCode))
                    {
                        // transient network error -> notify and retry with backoff
                        ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs { ErrorText = $"Transient network error: {rex.Message}. Reconnecting..." });

                        // Ensure the state reflects that we're about to retry
                        SetConnectionState(ConnectionState.Reconnecting);

                        int delayMs = ComputeBackoffMs(attempt);
                        try
                        {
                            await Task.Delay(delayMs, linkedToken);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }

                        // retry on next loop iteration
                    }
                    catch (RpcException rex)
                    {
                        // Non-transient RPC error -> surface and stop
                        ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs { ErrorText = $"RPC error: {rex.Message}" });
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Unexpected error -> surface and stop
                        ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs { ErrorText = ex.Message });
                        break;
                    }
                }
            }
            finally
            {
                // Ensure request channel is completed when receive loop finishes
                try
                {
                    _requestChannel?.Writer.TryComplete();
                }
                catch (Exception ex)
                {
                    ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs { ErrorText = ex.Message });
                }

                // Final state is Disconnected
                SetConnectionState(ConnectionState.Disconnected);
            }
        }, CancellationToken.None);

        await Task.CompletedTask;
    }

    private void SetConnectionState(ConnectionState newState)
    {
        ConnectionState prev;
        bool changed = false;
        lock (_stateLock)
        {
            prev = _connectionState;
            if (prev != newState)
            {
                _connectionState = newState;
                changed = true;
            }
        }

        if (changed)
        {
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs { OldState = prev, NewState = newState });
        }
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
        // exponential backoff base 500ms, cap at 8000ms, add jitter +/-20%
        if (attempt <= 0) attempt = 1;
        double baseMs = 500.0 * Math.Pow(2.0, Math.Min(attempt - 1, 4)); // caps at 500*16 = 8000
        int capMs = 8000;
        int ms = (int)Math.Min(baseMs, capMs);
        // Add jitter of up to +/-20%
        double jitter = (Random.Shared.NextDouble() * 0.4) - 0.2; // [-0.2, +0.2]
        ms = (int)Math.Max(100, ms + ms * jitter);
        return ms;
    }

    public async Task SendRequestTripAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        await SendAsync(new UserTripPacket
        {
            Type = UserTripPacketType.RequestTrip,
            Trip = trip,
        }, cancellationToken);
    }

    public async Task SendCancelTripAsync(CancellationToken cancellationToken = default)
    {
        await SendAsync(new UserTripPacket
        {
            Type = UserTripPacketType.CancelTrip
        }, cancellationToken);
    }
    
    
    public async Task SendAsync(UserTripPacket packet, CancellationToken cancellationToken = default)
    {
        if (_requestChannel is null) throw new InvalidOperationException("Not connected");
        await _requestChannel.Writer.WriteAsync(packet, cancellationToken).ConfigureAwait(false);
    }

    public async Task Disconnect()
    {
        if(_cts is not null)
            await _cts.CancelAsync();
        _requestChannel?.Writer.TryComplete();
        if (_receiveLoopTask is not null)
        {
            try
            {
                await _receiveLoopTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs { ErrorText = ex.Message });
            }
        }
        _requestChannel = null;
        _cts?.Dispose();
        _cts = null;

        // Ensure state is disconnected when explicitly disconnected
        SetConnectionState(ConnectionState.Disconnected);
    }


}


public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting
}

public class StatusUpdateEventArgs : EventArgs
{
    public Trip? Trip { get; set; }
}
public class ErrorReceivedEventArgs : EventArgs
{
    public string ErrorText { get; set; } = string.Empty;
}
public class NotificationReceivedEventArgs : EventArgs
{
    public string NotificationText { get; set; } = string.Empty;
}
public class DriverLocationsReceivedEventArgs : EventArgs
{
    public List<GLocation> Locations { get; set; } = [];
}

public class ConnectionStateChangedEventArgs : EventArgs
{
    public ConnectionState OldState { get; set; }
    public ConnectionState NewState { get; set; }
}
