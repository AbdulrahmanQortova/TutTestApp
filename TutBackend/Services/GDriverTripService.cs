using Grpc.Core;
using ProtoBuf.Grpc;
using System.Threading.Channels;
using Tut.Common.GServices;
using Tut.Common.Models;
using Tut.Common.Utils;
using TutBackend.Repositories;
namespace TutBackend.Services;

public class GDriverTripService(
    IDriverRepository driverRepository,
    ITripRepository tripRepository,
    QipClient qipClient,
    ILogger<GDriverManagerService> logger
    )
    : IGDriverTripService
{
    private Channel<DriverTripPacket>? _responseChannel;
    private Driver? _driver;
    private readonly CancellationTokenSource _cancellation = new ();
    private Trip? _activeTrip;
    public async IAsyncEnumerable<DriverTripPacket> Connect(IAsyncEnumerable<DriverTripPacket> requestPackets, CallContext context = default)
    {
        var driver = await AuthUtils.AuthorizeDriver(context, driverRepository, qipClient);
        _driver = driver ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthorized"));

        _driver.State = DriverState.Inactive;
        await driverRepository.UpdateAsync(_driver);

        _responseChannel = Channel.CreateBounded<DriverTripPacket>(new BoundedChannelOptions(20)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        // Background task: consume incoming request packets and act on them.
        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var packet in requestPackets.WithCancellation(_cancellation.Token))
                {
                    DriverTripPacket res = await DispatchIncomingPacket(packet);
                    await _responseChannel.Writer.WriteAsync(res, _cancellation.Token);
                }
            }
            catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
            {
                // expected cancellation
            }
            catch (Exception)
            {
                // Swallow all other exceptions
            }
            await _cancellation.CancelAsync();
            _cancellation.Dispose();
        }, CancellationToken.None);

        _ = Task.Run(() => WaitForTripAsync(_cancellation.Token), CancellationToken.None);

        // Subscribe to response channel and yield packets as they become available
        var reader = _responseChannel.Reader;
        try
        {
            while (await reader.WaitToReadAsync(_cancellation.Token))
            {
                while (reader.TryRead(out var outPacket))
                {
                    yield return outPacket;
                }
            }
        }
        finally
        {
            // ensure channel is completed
            _responseChannel.Writer.TryComplete();
        }
        _driver.State = DriverState.Offline;
        await driverRepository.UpdateAsync(_driver);
    }

    private async Task WaitForTripAsync(CancellationToken cancellationToken = default)
    {
        DateTime offerSentTimeStamp = DateTime.MinValue;
        bool offerSent = false;
        int tripIdOffered = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
            if (_driver is null) continue;
            if (_responseChannel is null) continue;
            if (_driver.State != DriverState.Available) continue;
            
            Trip? trip = await tripRepository.GetActiveTripForDriver(_driver.Id);
            if (trip is null) continue;
            if (trip.Id != tripIdOffered || trip.Status != TripState.Requested)
            {
                offerSent = false;
                tripIdOffered = 0;
                offerSentTimeStamp = DateTime.MinValue;
            }
            else if (!offerSent || offerSentTimeStamp.Subtract(TimeSpan.FromMinutes(1)) > DateTime.UtcNow)
            {
                DriverTripPacket packet = new DriverTripPacket
                {
                    Type = DriverTripPacketType.OfferTrip,
                    Trip = trip
                };
                _activeTrip = trip;
                await _responseChannel.Writer.WriteAsync(packet, cancellationToken);
                
                logger.LogDebug("{Driver} sent offer trip {Trip}", _driver.FullName, trip.Id);
                
                offerSent = true;
                tripIdOffered = trip.Id;
                offerSentTimeStamp = DateTime.UtcNow;
            }
        }
    }

    private DriverTripPacket HandlePunchIn()
    {
        if (_driver is null) return new DriverTripPacket();
        if (_driver.State != DriverState.Inactive && _driver.State != DriverState.Offline)
        {
            return DriverTripPacket.Error($"You are already in {_driver.State.ToString()} state");
        }
        logger.LogDebug("Driver {Driver} punched in", _driver.FullName);
        _driver.State = DriverState.Available;
        driverRepository.UpdateAsync(_driver);
        return new DriverTripPacket();
    }
    
    private DriverTripPacket HandlePunchOut()
    {
        if (_driver is null) return new DriverTripPacket();
        if (_driver.State != DriverState.Available)
        {
            return DriverTripPacket.Error($"You can't punch out when your already in {_driver.State.ToString()} state");
        }
        logger.LogDebug("Driver {Driver} punched out", _driver.FullName);
        _driver.State = DriverState.Inactive;
        driverRepository.UpdateAsync(_driver);
        return new DriverTripPacket();
    }

    private DriverTripPacket HandleTripReceived()
    {
        if (_driver is null) return new DriverTripPacket();
        if (_driver.State != DriverState.Requested)
        {
            logger.LogError("Driver {Driver} Ack Trip Received while he is in State: {State}", _driver.FullName, _driver.State.ToString());
            return new DriverTripPacket();
        }
        if (_activeTrip is null)
        {
            logger.LogError("Driver {Driver} Ack Trip Received while there are no active trip for him", _driver.FullName);
            return new DriverTripPacket();
        }
        logger.LogDebug("Driver {Driver} Ack Trip {Trip} Received", _driver.FullName, _activeTrip.Id);
        return new DriverTripPacket();
    }

    private async Task<DriverTripPacket> HandleAcceptTripAsync()
    {
        if (_driver is null) return new DriverTripPacket();
        if (_driver.State != DriverState.Requested)
        {
            logger.LogError("Driver {Driver} Ack Trip Received while he is in State: {State}", _driver.FullName, _driver.State.ToString());
            return new DriverTripPacket();
        }
        if (_activeTrip is null)
        {
            logger.LogError("Driver {Driver} Accepted Trip while there are no active trip for him", _driver.FullName);
            return new DriverTripPacket();
        }
        
        logger.LogDebug("Driver {Driver} Accepted Trip {Trip}", _driver.FullName, _activeTrip.Id);

        _driver.State = DriverState.EnRoute;
        await driverRepository.UpdateAsync(_driver);
        _activeTrip.Status = TripState.Accepted;
        await tripRepository.UpdateAsync(_activeTrip);
        return DriverTripPacket.StatusUpdate(_activeTrip);
    }

    private async Task<DriverTripPacket> HandleDriverArrivedAsync()
    {
        if (_driver is null) return new DriverTripPacket();
        if (_driver.State != DriverState.EnRoute)
        {
            logger.LogError("Driver {Driver} Reported Arrival at pickup while he is in State: {State}", _driver.FullName, _driver.State.ToString());
            return new DriverTripPacket();
        }
        if (_activeTrip is null)
        {
            logger.LogError("Driver {Driver} Reported Arrival at pickup while there are no active trip for him", _driver.FullName);
            return new DriverTripPacket();
        }
        
        logger.LogDebug("Driver {Driver} Reported Arrival at pickup, Trip: {Trip}", _driver.FullName, _activeTrip.Id);
        
        _activeTrip.Status = TripState.DriverArrived;
        _activeTrip.DriverArrivalTime = DateTime.UtcNow;
        _activeTrip.ActualArrivalDuration = (_activeTrip.DriverArrivalTime - _activeTrip.CreatedAt).Seconds;
        await tripRepository.UpdateAsync(_activeTrip);
        return DriverTripPacket.StatusUpdate(_activeTrip);
    }

    private async Task<DriverTripPacket> HandleTripStartedAsync()
    {
        if (_driver is null) return new DriverTripPacket();
        if (_driver.State != DriverState.EnRoute)
        {
            logger.LogError("Driver {Driver} Started Trip while he is in State: {State}", _driver.FullName, _driver.State.ToString());
            return new DriverTripPacket();
        }
        if (_activeTrip is null)
        {
            logger.LogError("Driver {Driver} Started Trip while there are no active trip for him", _driver.FullName);
            return new DriverTripPacket();
        }
        
        logger.LogDebug("Driver {Driver} Started Trip: {Trip}", _driver.FullName, _activeTrip.Id);

        _activeTrip.Status = TripState.Started;
        _activeTrip.StartTime = DateTime.UtcNow;
        await tripRepository.UpdateAsync(_activeTrip);
        return DriverTripPacket.StatusUpdate(_activeTrip);
    }

    
    private async Task<DriverTripPacket> HandleTripStopProgressAsync(TripState tripState)
    {
        if (_driver is null) return new DriverTripPacket();
        if (_activeTrip is null)
        {
            logger.LogError("Driver {Driver} progressed Trip to {State} while there are no active trip for him", _driver.FullName, tripState.ToString());
            return new DriverTripPacket();
        }

        if (tripState == TripState.AtStop1)
        {
            TripState accurateState = TripState.AtStop1;
            switch (_activeTrip.Status)
            {
                case TripState.AfterStop1:
                    accurateState = TripState.AtStop2;
                    break;
                case TripState.AfterStop2:
                    accurateState = TripState.AtStop3;
                    break;
                case TripState.AfterStop3:
                    accurateState = TripState.AtStop4;
                    break;
                case TripState.AfterStop4:
                    accurateState = TripState.AtStop5;
                    break;
            }
            tripState = accurateState;
        }
        if (tripState == TripState.AfterStop1)
        {
            TripState accurateState = TripState.AfterStop1;
            switch (_activeTrip.Status)
            {
                case TripState.AtStop2:
                    accurateState = TripState.AfterStop2;
                    break;
                case TripState.AtStop3:
                    accurateState = TripState.AfterStop3;
                    break;
                case TripState.AtStop4:
                    accurateState = TripState.AfterStop4;
                    break;
                case TripState.AtStop5:
                    accurateState = TripState.AfterStop5;
                    break;
            }
            tripState = accurateState;
        }
        
        logger.LogDebug("Driver {Driver} Reported State: {State}, Trip: {Trip}", _driver.FullName, tripState, _activeTrip.Id);
        
        _activeTrip.Status = tripState;
        await tripRepository.UpdateAsync(_activeTrip);
        return DriverTripPacket.StatusUpdate(_activeTrip);
    }

    private async Task<DriverTripPacket> HandleArrivedAtDestinationAsync()
    {
        if (_driver is null) return new DriverTripPacket();
        if (_driver.State != DriverState.EnRoute)
        {
            logger.LogError("Driver {Driver} Reported arrive at destination while he is in State: {State}", _driver.FullName, _driver.State.ToString());
            return new DriverTripPacket();
        }
        if (_activeTrip is null)
        {
            logger.LogError("Driver {Driver} Reported arrive at destination while there are no active trip for him", _driver.FullName);
            return new DriverTripPacket();
        }
        
        logger.LogDebug("Driver {Driver} Arrived at Destination Trip: {Trip}", _driver.FullName, _activeTrip.Id);

        _activeTrip.Status = TripState.Arrived;
        _activeTrip.EndTime = DateTime.UtcNow;
        _activeTrip.ActualTripDuration = (_activeTrip.EndTime - _activeTrip.StartTime).Seconds;
        _activeTrip.ActualDistance = _activeTrip.EstimatedDistance;
        _activeTrip.ActualCost = _activeTrip.EstimatedCost;
        await tripRepository.UpdateAsync(_activeTrip);
        return DriverTripPacket.StatusUpdate(_activeTrip);
    }
    
    private async Task<DriverTripPacket> HandlePaymentMadeAsync()
    {
        if (_driver is null) return new DriverTripPacket();
        if (_activeTrip is null)
        {
            logger.LogError("Driver {Driver} ended Trip while there are no active trip for him", _driver.FullName);
            return new DriverTripPacket();
        }
        
        logger.LogDebug("Driver {Driver} Reported Payment, Trip: {Trip}, Amount: {Amount}", _driver.FullName, _activeTrip.Id, _activeTrip.ActualCost);
        
        _driver.State = DriverState.Available;
        await driverRepository.UpdateAsync(_driver);
        _activeTrip.Status = TripState.Ended;
        await tripRepository.UpdateAsync(_activeTrip);

        _activeTrip = null;
        
        return DriverTripPacket.StatusUpdate(_activeTrip);
    }
    
    
    
    private async Task<DriverTripPacket> DispatchIncomingPacket(DriverTripPacket packet)
    {
        try
        {
            switch (packet.Type)
            {
                case DriverTripPacketType.PunchIn:
                    return HandlePunchIn();
                case DriverTripPacketType.PunchOut:
                    return HandlePunchOut();
                case DriverTripPacketType.TripReceived:
                    return HandleTripReceived();
                case DriverTripPacketType.AcceptTrip:
                    return await HandleAcceptTripAsync();

                case DriverTripPacketType.ArrivedAtPickup:
                    return await HandleDriverArrivedAsync();
                case DriverTripPacketType.StartTrip:
                    return await HandleTripStartedAsync();
                case DriverTripPacketType.ArrivedAtStop:
                    return await HandleTripStopProgressAsync(TripState.AtStop1);
                case DriverTripPacketType.ContinueTrip:
                    return await HandleTripStopProgressAsync(TripState.AfterStop1);
                case DriverTripPacketType.ArrivedAtDestination:
                    return await HandleArrivedAtDestinationAsync();
                case DriverTripPacketType.CashPaymentMade:
                    return await HandlePaymentMadeAsync();
                case DriverTripPacketType.GetStatus:
                    return DriverTripPacket.StatusUpdate(_activeTrip);

                default:
                    // Unknown packet
                    logger.LogError("Unknown Packet Type:\n{Packet}", packet.ToJson());
                    return DriverTripPacket.Error("Unknown packet type");
            }
        }
        catch (Exception)
        {
            // If processing a single packet fails, send an Error packet back
                return DriverTripPacket.Error("Error while processing driver packets");
        }
    }

}
