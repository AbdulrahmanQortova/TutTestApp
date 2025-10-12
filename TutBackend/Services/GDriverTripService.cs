using Grpc.Core;
using ProtoBuf.Grpc;
using System.Threading.Channels;
using Tut.Common.Business;
using Tut.Common.GServices;
using Tut.Common.Models;
using Tut.Common.Utils;
using TutBackend.Repositories;
namespace TutBackend.Services;

public class GDriverTripService(
    IDriverRepository driverRepository,
    ITripRepository tripRepository,
    IDriverLocationRepository driverLocationRepository,
    QipClient qipClient,
    IPricingStrategy pricingStrategy,
    IServiceScopeFactory scopeFactory,
    ILogger<GDriverManagerService> logger
    )
    : IGDriverTripService
{
    private Channel<DriverTripPacket>? _responseChannel;
    private int _driverId = -1;
    private int _activeTripId = -1;
    private CancellationTokenSource? _cancellation;

    // Offer tracking moved to instance fields to reduce method complexity and avoid ref/out in async helpers
    private DateTime _offerSentTimeStamp = DateTime.MinValue;
    private bool _offerSent;
    private int _tripIdOffered;

    private const int WaitForTripDelay = 1000;  // Milliseconds
    private const int ReOfferDelay = 60;        // Seconds

    public async IAsyncEnumerable<DriverTripPacket> Connect(IAsyncEnumerable<DriverTripPacket> requestPackets, CallContext context = default)
    {
        var driver = await AuthUtils.AuthorizeDriver(context, driverRepository, qipClient);
        var authorizedDriver = driver ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthorized"));

        authorizedDriver.State = DriverState.Inactive;
        await driverRepository.UpdateAsync(authorizedDriver);
        _driverId = authorizedDriver.Id;

        // Create cancellation token source linked to the gRPC context cancellation token
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);

        _responseChannel = Channel.CreateBounded<DriverTripPacket>(new BoundedChannelOptions(20)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        // Driver is already on Trip, send him status update and adjust his state accordingly
        Trip? trip = await tripRepository.GetActiveTripForDriver(authorizedDriver.Id);
        if (trip is not null)
        {
            _activeTripId = trip.Id;
            authorizedDriver.State = GetDriverStateForTripState(trip.Status);
            await driverRepository.UpdateAsync(authorizedDriver);
        }

        await _responseChannel.Writer.WriteAsync(DriverTripPacket.StatusUpdate(trip), _cancellation.Token);

        // Store the cancellation token to avoid capturing the CTS in Task.Run
        var cancellationToken = _cancellation.Token;
        
        // Background task: consume incoming request packets and act on them.
        _ = Task.Run(async () => await ProcessIncomingRequestsAsync(requestPackets, cancellationToken), CancellationToken.None);
        _ = Task.Run(async () => await WaitForTripAsync(cancellationToken), CancellationToken.None);

        // Subscribe to response channel and yield packets as they become available
        // Note: yield return cannot be in try/catch, so we handle cancellation through the async enumerable
        await foreach (DriverTripPacket outPacket in _responseChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            if (outPacket.Type != DriverTripPacketType.Unspecified && outPacket.Type != DriverTripPacketType.Success)
                yield return outPacket;
        }
        
        // Cleanup: This will run when the foreach completes (either normally or via cancellation)
        await CleanupConnectionAsync();
    }

    private async Task CleanupConnectionAsync()
    {
        // ensure channel is completed
        _responseChannel?.Writer.TryComplete();

        // Cancel and dispose the cancellation token source
        if (_cancellation is not null)
        {
            try
            {
                await _cancellation.CancelAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to cancel driver trip service cancellation token");
            }
            _cancellation.Dispose();
            _cancellation = null;
        }

        // Update driver state offline using a fresh scope to avoid cross-thread DbContext access
        using var scope = scopeFactory.CreateScope();
        IDriverRepository scopedDriverRepo = scope.ServiceProvider.GetRequiredService<IDriverRepository>();
        if (_driverId != -1)
        {
            Driver? scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
            if (scopedDriver is not null)
            {
                scopedDriver.State = DriverState.Offline;
                await scopedDriverRepo.UpdateAsync(scopedDriver);
                logger.LogInformation("Driver {Driver} set to Offline state on disconnect", scopedDriver.FullName);
            }
        }
    }

    private async Task ProcessIncomingRequestsAsync(IAsyncEnumerable<DriverTripPacket> requestPackets, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var packet in requestPackets.WithCancellation(cancellationToken))
            {
                DriverTripPacket res = await DispatchIncomingPacket(packet);
                if (_responseChannel != null)
                    await _responseChannel.Writer.WriteAsync(res, cancellationToken);
            }
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            // expected cancellation
            logger.LogDebug(ex, "ProcessIncomingRequestsAsync cancelled for driver {DriverId}", _driverId);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Exception while ProcessingIncomingRequests in DriverTripService");
        }
        finally
        {
            // Complete the channel writer so the main loop can exit
            _responseChannel?.Writer.TryComplete();
            logger.LogDebug("ProcessIncomingRequestsAsync completed for driver {DriverId}", _driverId);
        }
    }

    private async Task WaitForTripAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(WaitForTripDelay, cancellationToken);
                if (_driverId == -1 || _responseChannel is null) continue;
                
                await TryCheckAndOfferAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            // expected cancellation
            logger.LogDebug(ex, "WaitForTripAsync cancelled for driver {DriverId}", _driverId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in WaitForTripAsync");
        }
    }

    private async Task TryCheckAndOfferAsync(CancellationToken cancellationToken)
    {
        // Fetch fresh driver to check current state
        using var scope = scopeFactory.CreateScope();
        var scopedTripRepo = scope.ServiceProvider.GetRequiredService<ITripRepository>();
        var scopedDriverRepo = scope.ServiceProvider.GetRequiredService<IDriverRepository>();
        var scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        if (scopedDriver is null || scopedDriver.State != DriverState.Requested) return;

        Trip? trip = await scopedTripRepo.GetActiveTripForDriver(_driverId);

        if (trip is null || trip.Status != TripState.Requested)
        {
            _offerSent = false;
            _tripIdOffered = 0;
            _offerSentTimeStamp = DateTime.MinValue;
            return;
        }
        if (_tripIdOffered != trip.Id)
        {
            // The offered trip has changed, first trip is not valid for some reason and system assigned a new one.
            // Reset the offer state and start from scratch.
            _offerSent = false;
            _tripIdOffered = 0;
            _offerSentTimeStamp = DateTime.MinValue;
        }
        
        if (_offerSent && _offerSentTimeStamp.AddSeconds(ReOfferDelay) >= DateTime.UtcNow)
            return;
        
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

    private DriverTripPacket? InTripSanityChecks(Driver? driver, Trip? trip, string action, DriverState requiredDriverState, TripState requiredTripState)
    {
        if (driver is null) return DriverTripPacket.Error($"Driver# {_driverId} disappeared from database");
        if (driver.State != requiredDriverState)
        {
            logger.LogError("Driver {Driver} {Action} while he is in State: {State}", driver.FullName, action, driver.State.ToString());
            return DriverTripPacket.Error($"Driver {action} while he is in State: {driver.State}");
        }
        if (trip is null)
        {
            logger.LogError("Driver {Driver} {Action} while there are no active trip for him", driver.FullName, action);
            return DriverTripPacket.Error($"Driver {action} while there are no active trip for him");
        }
        if (trip.Status != requiredTripState)
        {
            logger.LogError("Driver {Driver} {Action} while Trip was in State: {State}", driver.FullName, action, trip.Status);
            return DriverTripPacket.Error($"Driver {action} while Trip was in State {trip.Status}");
        }
        return null;
    }

    private async Task<DriverTripPacket> HandlePunchInAsync(IDriverRepository scopedDriverRepo)
    {
        var scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        if (scopedDriver is null) return DriverTripPacket.Error($"Driver# {_driverId} disappeared from database");
        if (scopedDriver.State != DriverState.Inactive && scopedDriver.State != DriverState.Offline)
        {
            return DriverTripPacket.Error($"You are already in {scopedDriver.State.ToString()} state");
        }
        logger.LogDebug("Driver {Driver} punched in", scopedDriver.FullName);
        scopedDriver.State = DriverState.Available;
        await scopedDriverRepo.UpdateAsync(scopedDriver);
        return DriverTripPacket.Success();
    }
    
    private async Task<DriverTripPacket> HandlePunchOutAsync(IDriverRepository scopedDriverRepo)
    {
        var scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        if (scopedDriver is null) return DriverTripPacket.Error($"Driver# {_driverId} disappeared from database");
        if (scopedDriver.State != DriverState.Available)
        {
            return DriverTripPacket.Error($"You can't punch out when your in {scopedDriver.State.ToString()} state");
        }
        logger.LogDebug("Driver {Driver} punched out", scopedDriver.FullName);
        scopedDriver.State = DriverState.Inactive;
        await scopedDriverRepo.UpdateAsync(scopedDriver);
        return DriverTripPacket.Success();
    }

    private async Task<DriverTripPacket> HandleTripReceived(IDriverRepository scopedDriverRepo, ITripRepository scopedTripRepo)
    {
        Driver? scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        Trip? scopedTrip = _activeTripId == -1 ? null : await scopedTripRepo.GetActiveTripForDriver(_driverId);
        DriverTripPacket? sanityResult = InTripSanityChecks(scopedDriver, scopedTrip, "Ack Trip Received", DriverState.Requested, TripState.Requested);
        if (sanityResult is not null) return sanityResult;
        
        logger.LogDebug("Driver {Driver} Ack Trip {Trip} Received", scopedDriver!.FullName, scopedTrip!.Id);
        scopedTrip.Status = TripState.Acknowledged;
        await scopedTripRepo.UpdateAsync(scopedTrip);
        return DriverTripPacket.Success();
    }

    private async Task<DriverTripPacket> HandleAcceptTripAsync(IDriverRepository scopedDriverRepo, ITripRepository scopedTripRepo)
    {
        Driver? scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        Trip? scopedTrip = await scopedTripRepo.GetActiveTripForDriver(_driverId);
        DriverTripPacket? sanityResult = InTripSanityChecks(scopedDriver, scopedTrip, "Accepted Trip", DriverState.Requested, TripState.Acknowledged);
        if (sanityResult is not null) return sanityResult;

        logger.LogDebug("!!Driver {Driver} Accepted Trip {Trip}", scopedDriver!.FullName, scopedTrip!.Id);
        scopedDriver.State = DriverState.EnRoute;
        await scopedDriverRepo.UpdateAsync(scopedDriver);
        scopedTrip.Status = TripState.Accepted;
        DriverLocation? driverLocation = await driverLocationRepository.GetLatestDriverLocation(scopedDriver.Id);
        if (driverLocation is not null)
        {
            scopedTrip.RequestedDriverPlace = new Place
            {
                PlaceType = PlaceType.Location,
                Name = "Requested Driver Place",
                Latitude = driverLocation.Latitude,
                Longitude = driverLocation.Longitude,
            };
        }
        await scopedTripRepo.UpdateAsync(scopedTrip);
        return DriverTripPacket.StatusUpdate(scopedTrip);
    }

    private async Task<DriverTripPacket> HandleDriverArrivedAsync(IDriverRepository scopedDriverRepo, ITripRepository scopedTripRepo)
    {
        Driver? scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        Trip? scopedTrip = await scopedTripRepo.GetActiveTripForDriver(_driverId);
        DriverTripPacket? sanityResult = InTripSanityChecks(scopedDriver, scopedTrip, "Reported Arrival at pickup", DriverState.EnRoute, TripState.Accepted);
        if (sanityResult is not null) return sanityResult;
        
        logger.LogDebug("Driver {Driver} Reported Arrival at pickup, Trip: {Trip}", scopedDriver!.FullName, scopedTrip!.Id);
        
        scopedTrip.Status = TripState.DriverArrived;
        scopedTrip.DriverArrivalTime = DateTime.UtcNow;
        scopedTrip.ActualArrivalDuration = (int)((scopedTrip.DriverArrivalTime - scopedTrip.CreatedAt).TotalSeconds);
        scopedTrip.NextStop++;
        await scopedTripRepo.UpdateAsync(scopedTrip);
        return DriverTripPacket.StatusUpdate(scopedTrip);
    }

    private async Task<DriverTripPacket> HandleTripStartedAsync(IDriverRepository scopedDriverRepo, ITripRepository scopedTripRepo)
    {
        Driver? scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        Trip? scopedTrip = await scopedTripRepo.GetActiveTripForDriver(_driverId);
        DriverTripPacket? sanityResult = InTripSanityChecks(scopedDriver, scopedTrip, "Started Trip", DriverState.EnRoute, TripState.DriverArrived);
        if (sanityResult is not null) return sanityResult;
        
        logger.LogDebug("Driver {Driver} Started Trip: {Trip}", scopedDriver!.FullName, scopedTrip!.Id);

        scopedDriver.State = DriverState.OnTrip;
        await scopedDriverRepo.UpdateAsync(scopedDriver);
        scopedTrip.Status = TripState.Ongoing;
        scopedTrip.StartTime = DateTime.UtcNow;
        await scopedTripRepo.UpdateAsync(scopedTrip);
        return DriverTripPacket.StatusUpdate(scopedTrip);
    }

    private async Task<DriverTripPacket> HandleTripAtStopAsync(IDriverRepository scopedDriverRepo, ITripRepository scopedTripRepo)
    {
        Driver? scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        Trip? scopedTrip = await scopedTripRepo.GetActiveTripForDriver(_driverId);
        DriverTripPacket? sanityResult = InTripSanityChecks(scopedDriver, scopedTrip, "Arrived At Stop", DriverState.OnTrip, TripState.Ongoing);
        if (sanityResult is not null) return sanityResult;
        
        logger.LogDebug("Driver {Driver} Reported Arriving At Stop, Trip: {Trip}", scopedDriver!.FullName, scopedTrip!.Id);
        
        scopedTrip.Status = TripState.AtStop;
        scopedTrip.NextStop++;
        await scopedTripRepo.UpdateAsync(scopedTrip);
        return DriverTripPacket.StatusUpdate(scopedTrip);
    }

    private async Task<DriverTripPacket> HandleTripContinueAsync(IDriverRepository scopedDriverRepo, ITripRepository scopedTripRepo)
    {
        Driver? scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        Trip? scopedTrip = await scopedTripRepo.GetActiveTripForDriver(_driverId);
        DriverTripPacket? sanityResult = InTripSanityChecks(scopedDriver, scopedTrip, "Continued after Stop", DriverState.OnTrip, TripState.AtStop);
        if (sanityResult is not null) return sanityResult;
        
        logger.LogDebug("Driver {Driver} Continued after Stop Trip: {Trip}", scopedDriver!.FullName, scopedTrip!.Id);

        scopedTrip.Status = TripState.Ongoing;
        await scopedTripRepo.UpdateAsync(scopedTrip);
        return DriverTripPacket.StatusUpdate(scopedTrip);
    }

    private async Task<DriverTripPacket> HandleArrivedAtDestinationAsync(IDriverRepository scopedDriverRepo, ITripRepository scopedTripRepo)
    {
        Driver? scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        Trip? scopedTrip = await scopedTripRepo.GetActiveTripForDriver(_driverId);
        DriverTripPacket? sanityResult = InTripSanityChecks(scopedDriver, scopedTrip, "Arrived At Destination", DriverState.OnTrip, TripState.Ongoing);
        if (sanityResult is not null) return sanityResult;

        logger.LogDebug("Driver {Driver} Arrived at Destination Trip: {Trip}", scopedDriver!.FullName, scopedTrip!.Id);

        scopedTrip.Status = TripState.Arrived;
        scopedTrip.EndTime = DateTime.UtcNow;
        scopedTrip.ActualTripDuration = (int)((scopedTrip.EndTime - scopedTrip.StartTime).TotalSeconds);
        scopedTrip.ActualDistance = scopedTrip.EstimatedDistance;
        scopedTrip.ActualCost = pricingStrategy.FinalPrice(scopedTrip);
        await scopedTripRepo.UpdateAsync(scopedTrip);
        return DriverTripPacket.StatusUpdate(scopedTrip);
    }
    
    private async Task<DriverTripPacket> HandlePaymentMadeAsync(IDriverRepository scopedDriverRepo, ITripRepository scopedTripRepo)
    {
        Driver? scopedDriver = await scopedDriverRepo.GetByIdAsync(_driverId);
        Trip? scopedTrip = await scopedTripRepo.GetActiveTripForDriver(_driverId);
        DriverTripPacket? sanityResult = InTripSanityChecks(scopedDriver, scopedTrip, "Reported Receive Payment", DriverState.OnTrip, TripState.Arrived);
        if (sanityResult is not null) return sanityResult;
        
        logger.LogDebug("Driver {Driver} Reported Payment, Trip: {Trip}, Amount: {Amount}", scopedDriver!.FullName, scopedTrip!.Id, scopedTrip.ActualCost);
        
        scopedDriver.State = DriverState.Available;
        await scopedDriverRepo.UpdateAsync(scopedDriver);
        scopedTrip.Status = TripState.Ended;
        await scopedTripRepo.UpdateAsync(scopedTrip);
        _activeTripId = -1;
        return DriverTripPacket.StatusUpdate(scopedTrip);
    }
    
    private async Task<DriverTripPacket> DispatchIncomingPacket(DriverTripPacket packet)
    {
        try
        {
            // Resolve scoped repositories per incoming packet to avoid sharing DbContext across threads
            using var scope = scopeFactory.CreateScope();
            var scopedDriverRepo = scope.ServiceProvider.GetRequiredService<IDriverRepository>();
            var scopedTripRepo = scope.ServiceProvider.GetRequiredService<ITripRepository>();

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
                    return await HandleTripAtStopAsync(scopedDriverRepo, scopedTripRepo);
                case DriverTripPacketType.ContinueTrip:
                    return await HandleTripContinueAsync(scopedDriverRepo, scopedTripRepo);
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

    private DriverState GetDriverStateForTripState(TripState state)
    {
        switch (state)
        {
            case TripState.Requested:
                return DriverState.Requested;
            case TripState.Acknowledged:
            case TripState.Accepted:
            case TripState.DriverArrived:
                return DriverState.EnRoute;
            case TripState.Ongoing:
            case TripState.AtStop:
            case TripState.Arrived: 
                return DriverState.OnTrip;
            case TripState.Ended:
            case TripState.Canceled:
                return DriverState.Available;
            case TripState.Unspecified:
            default:
                return DriverState.Unspecified;
        }
    }
}
