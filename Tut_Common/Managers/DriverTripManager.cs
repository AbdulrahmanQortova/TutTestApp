using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;
using System.Threading.Channels;
using Tut.Common.GServices;
using Tut.Common.Models;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Tut.Common.Managers;

public class DriverTripManager
{
    private readonly IGDriverTripService _driverTripService;
    private Channel<DriverTripPacket>? _requestChannel;
    private CancellationTokenSource? _cts;
    private Task? _receiveLoopTask;

    private readonly Lock _stateLock = new();
    private ConnectionState _connectionState = ConnectionState.Disconnected;
    private Trip? _currentTrip;

    private CallOptions _callOptions;
    public ConnectionState CurrentState
    {
        get { lock (_stateLock) { return _connectionState; } }
    }

    public Trip? CurrentTrip
    {
        get { lock (_stateLock) { return _currentTrip; } }
        private set { lock (_stateLock) { _currentTrip = value; } }
    }

    public event EventHandler<StatusUpdateEventArgs>? StatusChanged;
    public event EventHandler<StatusUpdateEventArgs>? OfferReceived;
    public event EventHandler<ErrorReceivedEventArgs>? ErrorReceived;
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    public DriverTripManager(string token, IGrpcChannelFactory channelFactory)
    {
        GrpcChannel grpcChannel = channelFactory.GetChannel();
        _driverTripService = grpcChannel.CreateGrpcService<IGDriverTripService>();
        Metadata metadata = [];
        metadata.Add("Authorization", $"Bearer {token}");
        _callOptions = new CallOptions(metadata);
    }

    public async Task Connect(CancellationToken cancellationToken)
    {
        if (_requestChannel is not null)
            return; // already connected

        _requestChannel = Channel.CreateBounded<DriverTripPacket>(new BoundedChannelOptions(20)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancellationToken linkedToken = _cts.Token;

        _receiveLoopTask = Task.Run(async () =>
        {
            int attempt = 0;
            try
            {
                while (!linkedToken.IsCancellationRequested)
                {
                    attempt++;
                    if (attempt == 1)
                        SetConnectionState(ConnectionState.Connecting);
                    else
                        SetConnectionState(ConnectionState.Reconnecting);

                    try
                    {
                        var requestStream = _requestChannel!.Reader.ReadAllAsync(linkedToken);
                        var responseStream = _driverTripService.Connect(requestStream, new CallContext(_callOptions));

                        attempt = 0;

                        SetConnectionState(ConnectionState.Connected);

                        await foreach (var packet in responseStream)
                        {
                            switch (packet.Type)
                            {
                                case DriverTripPacketType.Error:
                                    ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs { ErrorText = packet.ErrorText });
                                    break;
                                case DriverTripPacketType.StatusUpdate:
                                    CurrentTrip = packet.Trip;
                                    StatusChanged?.Invoke(this, new StatusUpdateEventArgs { Trip = packet.Trip });
                                    break;
                                case DriverTripPacketType.OfferTrip:
                                    CurrentTrip = packet.Trip;
                                    OfferReceived?.Invoke(this, new StatusUpdateEventArgs { Trip = packet.Trip });
                                    break;
                            }
                        }

                        // server closed stream gracefully
                        break;
                    }
                    catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (RpcException rex) when (IsTransient(rex.StatusCode))
                    {
                        ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs { ErrorText = $"Transient network error: {rex.Message}. Reconnecting..." });
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
        if (attempt <= 0) attempt = 1;
        double baseMs = 500.0 * Math.Pow(2.0, Math.Min(attempt - 1, 4));
        int capMs = 8000;
        int ms = (int)Math.Min(baseMs, capMs);
        double jitter = (Random.Shared.NextDouble() * 0.4) - 0.2;
        ms = (int)Math.Max(100, ms + ms * jitter);
        return ms;
    }

    public async Task SendPunchInAsync(CancellationToken cancellationToken = default)
    {
        await SendAsync(new DriverTripPacket { Type = DriverTripPacketType.PunchIn }, cancellationToken);
    }

    public async Task SendPunchOutAsync(CancellationToken cancellationToken = default)
    {
        await SendAsync(new DriverTripPacket { Type = DriverTripPacketType.PunchOut }, cancellationToken);
    }

    public async Task SendTripReceivedAsync(CancellationToken cancellationToken = default)
    {
        await SendAsync(new DriverTripPacket { Type = DriverTripPacketType.TripReceived, Trip = CurrentTrip }, cancellationToken);
    }

    public async Task SendAcceptTripAsync(CancellationToken cancellationToken = default)
    {
        await SendAsync(new DriverTripPacket { Type = DriverTripPacketType.AcceptTrip, Trip = CurrentTrip}, cancellationToken);
    }

    public async Task SendArrivedAtPickupAsync(CancellationToken cancellationToken = default)
    {
        await SendAsync(new DriverTripPacket { Type = DriverTripPacketType.ArrivedAtPickup, Trip = CurrentTrip}, cancellationToken);
    }

    public async Task SendStartTripAsync(CancellationToken cancellationToken = default)
    {
        await SendAsync(new DriverTripPacket { Type = DriverTripPacketType.StartTrip, Trip = CurrentTrip }, cancellationToken);
    }

    public async Task SendArrivedAtStopAsync(CancellationToken cancellationToken = default)
    {
        await SendAsync(new DriverTripPacket { Type = DriverTripPacketType.ArrivedAtStop, Trip = CurrentTrip }, cancellationToken);
    }

    public async Task SendContinueTripAsync(CancellationToken cancellationToken = default)
    {
        await SendAsync(new DriverTripPacket { Type = DriverTripPacketType.ContinueTrip, Trip = CurrentTrip }, cancellationToken);
    }

    public async Task SendArrivedAtDestinationAsync(CancellationToken cancellationToken = default)
    {
        await SendAsync(new DriverTripPacket { Type = DriverTripPacketType.ArrivedAtDestination, Trip = CurrentTrip }, cancellationToken);
    }

    public async Task SendCashPaymentMadeAsync(int amount, CancellationToken cancellationToken = default)
    {
        await SendAsync(new DriverTripPacket { Type = DriverTripPacketType.CashPaymentMade, Trip = CurrentTrip, PaymentAmount = amount }, cancellationToken);
    }

    public async Task SendGetStatusAsync(CancellationToken cancellationToken = default)
    {
        await SendAsync(new DriverTripPacket { Type = DriverTripPacketType.GetStatus }, cancellationToken);
    }

    public async Task SendAsync(DriverTripPacket packet, CancellationToken cancellationToken = default)
    {
        if (_requestChannel is null) throw new InvalidOperationException("Not connected");
        await _requestChannel.Writer.WriteAsync(packet, cancellationToken);
    }

    public async Task Disconnect()
    {
        _requestChannel?.Writer.TryComplete();
        if (_cts is not null)
            await _cts.CancelAsync();
        if (_receiveLoopTask is not null)
        {
            try
            {
                await _receiveLoopTask;
            }
            catch (Exception ex)
            {
                ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs { ErrorText = ex.Message });
            }
        }
        _requestChannel = null;
        _cts?.Dispose();
        _cts = null;

        SetConnectionState(ConnectionState.Disconnected);
    }
}
