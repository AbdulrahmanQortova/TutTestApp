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
    IServiceScopeFactory scopeFactory,
    ILogger<GDriverManagerService> logger
    )
    : IGDriverTripService
{
    private Channel<DriverTripPacket>? _responseChannel;
    // replaced sticky objects with ids to avoid keeping DbContext entities across threads
    private int _driverId = -1;
    private readonly CancellationTokenSource _cancellation = new ();
    private int _activeTripId = -1;

    // Offer tracking moved to instance fields to reduce method complexity and avoid ref/out in async helpers
    private DateTime _offerSentTimeStamp = DateTime.MinValue;
    private bool _offerSent;
    private int _tripIdOffered;

    public async IAsyncEnumerable<DriverTripPacket> Connect(IAsyncEnumerable<DriverTripPacket> requestPackets, CallContext context = default)
    {
        // Use the injected repositories for initial authorization within the request thread.
        var driver = await AuthUtils.AuthorizeDriver(context, driverRepository, qipClient);
        var authorizedDriver = driver ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthorized"));

        // Do initial update using the authorized driver instance, but only store the id for long-lived state
        authorizedDriver.State = DriverState.Inactive;
        await driverRepository.UpdateAsync(authorizedDriver);

        _driverId = authorizedDriver.Id;

        _responseChannel = Channel.CreateBounded<DriverTripPacket>(new BoundedChannelOptions(20)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        Trip? trip = await tripRepository.GetActiveTripForDriver(authorizedDriver.Id);
        if (trip is not null)
            _activeTripId = trip.Id;
        await _responseChannel.Writer.WriteAsync(DriverTripPacket.StatusUpdate(trip));

        // Background task: consume incoming request packets and act on them.
        _ = Task.Run(() => ProcessIncomingRequestsAsync(requestPackets), CancellationToken.None);

        _ = Task.Run(() => WaitForTripAsync(_cancellation.Token), CancellationToken.None);

        // Subscribe to response channel and yield packets as they become available
        var reader = _responseChannel.Reader;
        try
        {
            while (await reader.WaitToReadAsync(_cancellation.Token))
            {
                while (reader.TryRead(out var outPacket))
                {
                    if (outPacket.Type != DriverTripPacketType.Unspecified)
                        yield return outPacket;
                }
            }
        }
        finally
        {
            // ensure channel is completed
            _responseChannel.Writer.TryComplete();

            // Update driver state offline using a fresh scope to avoid cross-thread DbContext access
            try
            {
                using var scope = scopeFactory.CreateScope();
                var scopedDriverRepo = (IDriverRepository)scope.ServiceProvider.GetService(typeof(IDriverRepository))!;
                if (_driverId != -1)
                {
                    var scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
                    if (scopedDriver is not null)
                    {
                        scopedDriver.State = DriverState.Offline;
                        await scopedDriverRepo.UpdateAsync(scopedDriver);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to set driver offline in cleanup");
            }
        }
    }

    // New helper: moved the incoming request consumption loop here to reduce Connect complexity
    private async Task ProcessIncomingRequestsAsync(IAsyncEnumerable<DriverTripPacket> requestPackets)
    {
        try
        {
            await foreach (var packet in requestPackets.WithCancellation(_cancellation.Token))
            {
                DriverTripPacket res = await DispatchIncomingPacket(packet);
                if (_responseChannel != null)
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
        finally
        {
            try
            {
                await _cancellation.CancelAsync();
            }
            catch (Exception ex)
            {
                // Log cancellation failures - safe to continue cleanup
                logger.LogWarning(ex, "Failed to cancel process incoming requests cancellation token");
            }
            _cancellation.Dispose();
        }
    }

    private async Task WaitForTripAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
            if (_driverId == -1 || _responseChannel is null) continue;

            try
            {
                using var scope = scopeFactory.CreateScope();
                var scopedTripRepo = (ITripRepository)scope.ServiceProvider.GetService(typeof(ITripRepository))!;
                var scopedDriverRepo = (IDriverRepository)scope.ServiceProvider.GetService(typeof(IDriverRepository))!;

                await TryCheckAndOfferAsync(scopedDriverRepo, scopedTripRepo, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // expected
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while checking for trips in WaitForTripAsync");
            }
        }
    }

    // Extracted inner logic so WaitForTripAsync is simpler and cognitive complexity is reduced
    private async Task TryCheckAndOfferAsync(IDriverRepository scopedDriverRepo, ITripRepository scopedTripRepo, CancellationToken cancellationToken)
    {
        // Fetch fresh driver to check current state
        var scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        if (scopedDriver is null || scopedDriver.State != DriverState.Requested) return;

        Trip? trip = await scopedTripRepo.GetActiveTripForDriver(_driverId);

        if (trip is null)
        {
            _offerSent = false;
            _tripIdOffered = 0;
            _offerSentTimeStamp = DateTime.MinValue;
            return;
        }

        if (trip.Status != TripState.Requested)
        {
            _offerSent = false;
            _tripIdOffered = 0;
            _offerSentTimeStamp = DateTime.MinValue;
            return;
        }

        if (!_offerSent || _offerSentTimeStamp.AddMinutes(1) <= DateTime.UtcNow)
        {
            var packet = new DriverTripPacket { Type = DriverTripPacketType.OfferTrip, Trip = trip };
            _activeTripId = trip.Id;
            if (_responseChannel is not null)
                await _responseChannel.Writer.WriteAsync(packet, cancellationToken);

            var driverName = scopedDriver.FullName;
            logger.LogDebug("{Driver} sent offer trip {Trip}", driverName, trip.Id);

            _offerSent = true;
            _tripIdOffered = trip.Id;
            _offerSentTimeStamp = DateTime.UtcNow;
        }
    }

    private async Task<DriverTripPacket> HandlePunchInAsync(IDriverRepository scopedDriverRepo)
    {
        if (_driverId == -1) return new DriverTripPacket();
        var scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        if (scopedDriver is null) return new DriverTripPacket();
        if (scopedDriver.State != DriverState.Inactive && scopedDriver.State != DriverState.Offline)
        {
            return DriverTripPacket.Error($"You are already in {scopedDriver.State.ToString()} state");
        }
        logger.LogDebug("Driver {Driver} punched in", scopedDriver.FullName);
        scopedDriver.State = DriverState.Available;
        await scopedDriverRepo.UpdateAsync(scopedDriver);
        return new DriverTripPacket();
    }
    
    private async Task<DriverTripPacket> HandlePunchOutAsync(IDriverRepository scopedDriverRepo)
    {
        if (_driverId == -1) return new DriverTripPacket();
        var scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        if (scopedDriver is null) return new DriverTripPacket();
        if (scopedDriver.State != DriverState.Available)
        {
            return DriverTripPacket.Error($"You can't punch out when your already in {scopedDriver.State.ToString()} state");
        }
        logger.LogDebug("Driver {Driver} punched out", scopedDriver.FullName);
        scopedDriver.State = DriverState.Inactive;
        await scopedDriverRepo.UpdateAsync(scopedDriver);
        return new DriverTripPacket();
    }

    private async Task<DriverTripPacket> HandleTripReceived(IDriverRepository scopedDriverRepo, ITripRepository scopedTripRepo)
    {
        if (_driverId == -1) return new DriverTripPacket();
        var scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        Trip? scopedTrip = _activeTripId == -1 ? null : await scopedTripRepo.GetActiveTripForDriver(_driverId);
        if (scopedDriver is null) return new DriverTripPacket();
        if (scopedDriver.State != DriverState.Requested)
        {
            logger.LogError("Driver {Driver} Ack Trip Received while he is in State: {State}", scopedDriver.FullName, scopedDriver.State.ToString());
            return new DriverTripPacket();
        }
        if (scopedTrip is null)
        {
            logger.LogError("Driver {Driver} Ack Trip Received while there are no active trip for him", scopedDriver.FullName);
            return new DriverTripPacket();
        }
        logger.LogDebug("Driver {Driver} Ack Trip {Trip} Received", scopedDriver.FullName, scopedTrip.Id);

        scopedTrip.Status = TripState.Acknowledged;
        await scopedTripRepo.UpdateAsync(scopedTrip);

        return new DriverTripPacket();
    }

    private async Task<DriverTripPacket> HandleAcceptTripAsync(IDriverRepository scopedDriverRepo, ITripRepository scopedTripRepo)
    {
        if (_driverId == -1) return new DriverTripPacket();
        var scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        if (scopedDriver is null) return new DriverTripPacket();
        if (scopedDriver.State != DriverState.Requested)
        {
            logger.LogError("!!Driver {Driver} Accepted Trip while he is in State: {State}", scopedDriver.FullName, scopedDriver.State.ToString());
            return new DriverTripPacket();
        }
        if (_activeTripId == -1)
        {
            logger.LogError("!!Driver {Driver} Accepted Trip while there are no active trip for him", scopedDriver.FullName);
            return new DriverTripPacket();
        }
        
        var scopedTrip = await scopedTripRepo.GetActiveTripForDriver(_driverId);
        if (scopedTrip is null)
        {
            logger.LogError("!!Driver {Driver} Accepted Trip but trip not found, TripId: {TripId}", scopedDriver.FullName, _activeTripId);
            return new DriverTripPacket();
        }
        
        logger.LogDebug("!!Driver {Driver} Accepted Trip {Trip}", scopedDriver.FullName, scopedTrip.Id);

        scopedDriver.State = DriverState.EnRoute;
        await scopedDriverRepo.UpdateAsync(scopedDriver);
        scopedTrip.Status = TripState.Accepted;
        await scopedTripRepo.UpdateAsync(scopedTrip);
        return DriverTripPacket.StatusUpdate(scopedTrip);
    }

    private async Task<DriverTripPacket> HandleDriverArrivedAsync(IDriverRepository scopedDriverRepo, ITripRepository scopedTripRepo)
    {
        if (_driverId == -1) return new DriverTripPacket();
        var scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        if (scopedDriver is null) return new DriverTripPacket();
        if (scopedDriver.State != DriverState.EnRoute)
        {
            logger.LogError("Driver {Driver} Reported Arrival at pickup while he is in State: {State}", scopedDriver.FullName, scopedDriver.State.ToString());
            return new DriverTripPacket();
        }
        if (_activeTripId == -1)
        {
            logger.LogError("Driver {Driver} Reported Arrival at pickup while there are no active trip for him", scopedDriver.FullName);
            return new DriverTripPacket();
        }
        
        var scopedTrip = await scopedTripRepo.GetActiveTripForDriver(_driverId);
        if (scopedTrip is null)
        {
            logger.LogError("Driver {Driver} Reported Arrival but trip not found, TripId: {TripId}", scopedDriver.FullName, _activeTripId);
            return new DriverTripPacket();
        }
        
        logger.LogDebug("Driver {Driver} Reported Arrival at pickup, Trip: {Trip}", scopedDriver.FullName, scopedTrip.Id);
        
        scopedTrip.Status = TripState.DriverArrived;
        scopedTrip.DriverArrivalTime = DateTime.UtcNow;
        scopedTrip.ActualArrivalDuration = (scopedTrip.DriverArrivalTime - scopedTrip.CreatedAt).Seconds;
        await scopedTripRepo.UpdateAsync(scopedTrip);
        return DriverTripPacket.StatusUpdate(scopedTrip);
    }

    private async Task<DriverTripPacket> HandleTripStartedAsync(IDriverRepository scopedDriverRepo, ITripRepository scopedTripRepo)
    {
        if (_driverId == -1) return new DriverTripPacket();
        var scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        if (scopedDriver is null) return new DriverTripPacket();
        if (scopedDriver.State != DriverState.EnRoute)
        {
            logger.LogError("Driver {Driver} Started Trip while he is in State: {State}", scopedDriver.FullName, scopedDriver.State.ToString());
            return new DriverTripPacket();
        }
        if (_activeTripId == -1)
        {
            logger.LogError("Driver {Driver} Started Trip while there are no active trip for him", scopedDriver.FullName);
            return new DriverTripPacket();
        }
        
        var scopedTrip = await scopedTripRepo.GetActiveTripForDriver(_driverId);
        if (scopedTrip is null)
        {
            logger.LogError("Driver {Driver} Started Trip but trip not found, TripId: {TripId}", scopedDriver.FullName, _activeTripId);
            return new DriverTripPacket();
        }
        
        logger.LogDebug("Driver {Driver} Started Trip: {Trip}", scopedDriver.FullName, scopedTrip.Id);

        scopedTrip.Status = TripState.Started;
        scopedTrip.StartTime = DateTime.UtcNow;
        await scopedTripRepo.UpdateAsync(scopedTrip);
        return DriverTripPacket.StatusUpdate(scopedTrip);
    }

    private async Task<DriverTripPacket> HandleTripStopProgressAsync(IDriverRepository scopedDriverRepo, ITripRepository scopedTripRepo, TripState tripState)
    {
        if (_driverId == -1) return new DriverTripPacket();
        var scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        if (scopedDriver is null) return new DriverTripPacket();
        if (_activeTripId == -1)
        {
            logger.LogError("Driver {Driver} progressed Trip to {State} while there are no active trip for him", scopedDriver.FullName, tripState.ToString());
            return new DriverTripPacket();
        }

        var scopedTrip = await scopedTripRepo.GetActiveTripForDriver(_driverId);
        if (scopedTrip is null)
        {
            logger.LogError("Driver {Driver} progressed Trip but trip not found, TripId: {TripId}", scopedDriver.FullName, _activeTripId);
            return new DriverTripPacket();
        }

        if (tripState == TripState.AtStop1)
        {
            TripState accurateState = TripState.AtStop1;
            switch (scopedTrip.Status)
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
            switch (scopedTrip.Status)
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
        
        logger.LogDebug("Driver {Driver} Reported State: {State}, Trip: {Trip}", scopedDriver.FullName, tripState, scopedTrip.Id);
        
        scopedTrip.Status = tripState;
        await scopedTripRepo.UpdateAsync(scopedTrip);
        return DriverTripPacket.StatusUpdate(scopedTrip);
    }

    private async Task<DriverTripPacket> HandleArrivedAtDestinationAsync(IDriverRepository scopedDriverRepo, ITripRepository scopedTripRepo)
    {
        if (_driverId == -1) return new DriverTripPacket();
        var scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        if (scopedDriver is null) return new DriverTripPacket();
        if (scopedDriver.State != DriverState.EnRoute)
        {
            logger.LogError("Driver {Driver} Reported arrive at destination while he is in State: {State}", scopedDriver.FullName, scopedDriver.State.ToString());
            return new DriverTripPacket();
        }
        if (_activeTripId == -1)
        {
            logger.LogError("Driver {Driver} Reported arrive at destination while there are no active trip for him", scopedDriver.FullName);
            return new DriverTripPacket();
        }
        
        var scopedTrip = await scopedTripRepo.GetActiveTripForDriver(_driverId);
        if (scopedTrip is null)
        {
            logger.LogError("Driver {Driver} Reported arrive at destination but trip not found, TripId: {TripId}", scopedDriver.FullName, _activeTripId);
            return new DriverTripPacket();
        }
        
        logger.LogDebug("Driver {Driver} Arrived at Destination Trip: {Trip}", scopedDriver.FullName, scopedTrip.Id);

        scopedTrip.Status = TripState.Arrived;
        scopedTrip.EndTime = DateTime.UtcNow;
        scopedTrip.ActualTripDuration = (scopedTrip.EndTime - scopedTrip.StartTime).Seconds;
        scopedTrip.ActualDistance = scopedTrip.EstimatedDistance;
        scopedTrip.ActualCost = scopedTrip.EstimatedCost;
        await scopedTripRepo.UpdateAsync(scopedTrip);
        return DriverTripPacket.StatusUpdate(scopedTrip);
    }
    
    private async Task<DriverTripPacket> HandlePaymentMadeAsync(IDriverRepository scopedDriverRepo, ITripRepository scopedTripRepo)
    {
        if (_driverId == -1) return new DriverTripPacket();
        var scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        if (scopedDriver is null) return new DriverTripPacket();
        if (_activeTripId == -1)
        {
            logger.LogError("Driver {Driver} ended Trip while there are no active trip for him", scopedDriver.FullName);
            return new DriverTripPacket();
        }
        
        var scopedTrip = await scopedTripRepo.GetActiveTripForDriver(_driverId);
        if (scopedTrip is null)
        {
            logger.LogError("Driver {Driver} ended Trip but trip not found, TripId: {TripId}", scopedDriver.FullName, _activeTripId);
            return new DriverTripPacket();
        }
        
        logger.LogDebug("Driver {Driver} Reported Payment, Trip: {Trip}, Amount: {Amount}", scopedDriver.FullName, scopedTrip.Id, scopedTrip.ActualCost);
        
        scopedDriver.State = DriverState.Available;
        await scopedDriverRepo.UpdateAsync(scopedDriver);
        scopedTrip.Status = TripState.Ended;
        await scopedTripRepo.UpdateAsync(scopedTrip);

        _activeTripId = -1;
        
        return DriverTripPacket.StatusUpdate(null);
    }
    
    
    
    private async Task<DriverTripPacket> DispatchIncomingPacket(DriverTripPacket packet)
    {
        try
        {
            // Resolve scoped repositories per incoming packet to avoid sharing DbContext across threads
            using var scope = scopeFactory.CreateScope();
            var scopedDriverRepo = (IDriverRepository)scope.ServiceProvider.GetService(typeof(IDriverRepository))!;
            var scopedTripRepo = (ITripRepository)scope.ServiceProvider.GetService(typeof(ITripRepository))!;

            switch (packet.Type)
            {
                case DriverTripPacketType.PunchIn:
                    return await HandlePunchInAsync(scopedDriverRepo);
                case DriverTripPacketType.PunchOut:
                    return await HandlePunchOutAsync(scopedDriverRepo);
                case DriverTripPacketType.TripReceived:
                    return await HandleTripReceived(scopedDriverRepo, scopedTripRepo);
                case DriverTripPacketType.AcceptTrip:
                    return await HandleAcceptTripAsync(scopedDriverRepo, scopedTripRepo);

                case DriverTripPacketType.ArrivedAtPickup:
                    return await HandleDriverArrivedAsync(scopedDriverRepo, scopedTripRepo);
                case DriverTripPacketType.StartTrip:
                    return await HandleTripStartedAsync(scopedDriverRepo, scopedTripRepo);
                case DriverTripPacketType.ArrivedAtStop:
                    return await HandleTripStopProgressAsync(scopedDriverRepo, scopedTripRepo, TripState.AtStop1);
                case DriverTripPacketType.ContinueTrip:
                    return await HandleTripStopProgressAsync(scopedDriverRepo, scopedTripRepo, TripState.AfterStop1);
                case DriverTripPacketType.ArrivedAtDestination:
                    return await HandleArrivedAtDestinationAsync(scopedDriverRepo, scopedTripRepo);
                case DriverTripPacketType.CashPaymentMade:
                    return await HandlePaymentMadeAsync(scopedDriverRepo, scopedTripRepo);
                case DriverTripPacketType.GetStatus:
                {
                    if (_activeTripId == -1) return DriverTripPacket.StatusUpdate(null);
                    var scopedTrip = await scopedTripRepo.GetActiveTripForDriver(_driverId);
                    return DriverTripPacket.StatusUpdate(scopedTrip);
                }

                default:
                    // Unknown packet
                    logger.LogError("Unknown Packet Type:\n{Packet}", packet.ToJson());
                    return DriverTripPacket.Error("Unknown packet type");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while processing driver packets");
            // If processing a single packet fails, send an Error packet back
            return DriverTripPacket.Error("Error while processing driver packets");
        }
    }

}
