using Grpc.Core;
using ProtoBuf.Grpc;
using System.Threading.Channels;
using Tut.Common.Business;
using Tut.Common.GServices;
using Tut.Common.Managers;
using Tut.Common.Models;
using Tut.Common.Utils;
using TutBackend.Repositories;
namespace TutBackend.Services;

public class GUserTripService(
    IUserRepository userRepository,
    QipClient qipClient,
    IServiceScopeFactory scopeFactory,
    IPricingStrategy pricingStrategy,
    DriverSelector driverSelector,
    ILogger<GUserTripService> logger
    )
    : IGUserTripService
{

    private Channel<UserTripPacket>? _responseChannel;
    // store ids only to avoid holding entity instances across threads
    private int _userId = -1; // -1 indicates no authenticated user
    private CancellationTokenSource? _cancellation;

    public async IAsyncEnumerable<UserTripPacket> Connect(IAsyncEnumerable<UserTripPacket> requestPackets, CallContext context = default)
    {
        User? user = await AuthUtils.AuthorizeUser(context, userRepository, qipClient);
        var resolvedUser = user ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthorized"));
        _userId = resolvedUser.Id;

        // Create cancellation token source linked to the gRPC context cancellation token
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);

        _responseChannel = Channel.CreateBounded<UserTripPacket>(new BoundedChannelOptions(20)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        // Store the cancellation token to avoid capturing the CTS in Task.Run
        var cancellationToken = _cancellation.Token;

        // Background task: consume incoming request packets and act on them.
        _ = Task.Run(async () => await ProcessIncomingRequestsAsync(requestPackets, cancellationToken), CancellationToken.None);
        _ = Task.Run(async () => await ReportTripStatusUpdates(cancellationToken), CancellationToken.None);

        // Subscribe to response channel and yield packets as they become available
        await foreach (var outPacket in _responseChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            if(outPacket.Type != UserTripPacketType.Unspecified)
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
                logger.LogWarning(ex, "Failed to cancel user trip service cancellation token");
            }
            _cancellation.Dispose();
            _cancellation = null;
        }
        
        logger.LogInformation("User {UserId} disconnected from trip service", _userId);
    }

    private async Task ProcessIncomingRequestsAsync(IAsyncEnumerable<UserTripPacket> requestPackets, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var packet in requestPackets.WithCancellation(cancellationToken))
            {
                UserTripPacket res = await DispatchIncomingPacket(packet);
                if(_responseChannel != null)
                    await _responseChannel.Writer.WriteAsync(res, cancellationToken);
            }
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            // expected cancellation
            logger.LogDebug(ex, "ProcessIncomingRequestsAsync cancelled for user {UserId}", _userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while ProcessingIncomingRequests in UserTripService");
        }
        finally
        {
            // Complete the channel writer so the main loop can exit
            _responseChannel?.Writer.TryComplete();
            logger.LogDebug("ProcessIncomingRequestsAsync completed for user {UserId}", _userId);
        }
    }
    
    
    public void ProvideFeedback(Feedback feedback)
    {
        // Do nothing for now
    }
    
    public async Task<UserTripPacket> GetState()
    {
        // Return fresh state by resolving the active trip from a scoped repository
        if (_userId == -1) return UserTripPacket.Error("Unauthorized");
        using var scope = scopeFactory.CreateScope();
        var scopedTripRepo = scope.ServiceProvider.GetRequiredService<ITripRepository>();
        Trip? trip = await scopedTripRepo.GetActiveTripForUser(_userId);
        return UserTripPacket.StatusUpdate(trip);
    }


    private async Task ReportTripStatusUpdates(CancellationToken cancellationToken)
    {
        TripState lastCommunicatedState = TripState.Unspecified;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(10, cancellationToken);
                if (_userId == -1) continue;
                if (_responseChannel is null) continue;

                using var scope = scopeFactory.CreateScope();
                var scopedTripRepo = scope.ServiceProvider.GetRequiredService<ITripRepository>();
                lastCommunicatedState = await ProcessAndWriteTripStatusAsync(scopedTripRepo, lastCommunicatedState, cancellationToken);
            }
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            // expected cancellation
            logger.LogDebug(ex, "ReportTripStatusUpdates cancelled for user {UserId}", _userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while reporting trip status updates");
        }
    }

    // Extracted helper to process trip reporting for cognitive complexity reasons.
    private async Task<TripState> ProcessAndWriteTripStatusAsync(ITripRepository scopedTripRepo, TripState lastCommunicatedState, CancellationToken cancellationToken)
    {
        var currentTrip = await scopedTripRepo.GetActiveTripForUser(_userId);

        if (currentTrip is not null)
        {
            if (currentTrip.Status != lastCommunicatedState)
            {
                await _responseChannel!.Writer.WriteAsync(UserTripPacket.StatusUpdate(currentTrip), cancellationToken);
                lastCommunicatedState = currentTrip.Status;
            }
            return lastCommunicatedState;
        }

        // no active trip
        if (lastCommunicatedState == TripState.Unspecified) return lastCommunicatedState;
        await _responseChannel!.Writer.WriteAsync(UserTripPacket.StatusUpdate(null), cancellationToken);
        return TripState.Unspecified;
    }

    private async Task<UserTripPacket> HandleRequestTripAsync(ITripRepository scopedTripRepo, IUserRepository scopedUserRepo, Trip? trip)
    {
        if (trip is null) return UserTripPacket.Error("Trip Requested without specifying the trip");
        // Use fresh user entity from scoped repo and avoid storing trip entity globally
        Trip? prevTrip = await scopedTripRepo.GetActiveTripForUser(_userId);
        if (prevTrip is not null)
            return UserTripPacket.Error("User already has an active trip");
        User? user = await scopedUserRepo.GetByIdAsync(_userId);
        trip.User = user;
        await scopedTripRepo.AddAsync(trip);
        return UserTripPacket.Success();
    }
    
    private async Task<UserTripPacket> HandleCancelTripAsync(ITripRepository scopedTripRepo)
    {
        var currentTrip = await scopedTripRepo.GetActiveTripForUser(_userId);
        if (currentTrip is null)
        {
            return UserTripPacket.Error($"User# {_userId} Cancelled Trip while there was not trips for him");
        }

        currentTrip.Status = TripState.Canceled;
        currentTrip.CancelTime = DateTime.UtcNow;
        await scopedTripRepo.UpdateAsync(currentTrip);
        return UserTripPacket.StatusUpdate(currentTrip);
    }


    private async Task<UserTripPacket> HandleInquireTripAsync(Trip? trip)
    {
        if (trip is null) return UserTripPacket.Error("Trip Inquired without specifying the trip");

        trip.EstimatedCost = pricingStrategy.Price(trip);
        trip.EstimatedArrivalDuration = await driverSelector.EstimateDriverArrivalTime(trip);
        return new UserTripPacket
        {
            Type = UserTripPacketType.InquireResult,
            Trip = trip
        };
    }
    
    private async Task<UserTripPacket> DispatchIncomingPacket(UserTripPacket packet)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var scopedTripRepo = scope.ServiceProvider.GetRequiredService<ITripRepository>();
            var scopedUserRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            switch (packet.Type)
            {
                case UserTripPacketType.GetStatus:
                    // Resolve a fresh trip for status
                    if (_userId == -1) return UserTripPacket.Error("Unauthorized");
                    Trip? trip = await scopedTripRepo.GetActiveTripForUser(_userId);
                    return UserTripPacket.StatusUpdate(trip);
                case UserTripPacketType.RequestTrip:
                    return await HandleRequestTripAsync(scopedTripRepo, scopedUserRepo, packet.Trip);
                case UserTripPacketType.CancelTrip:
                    return await HandleCancelTripAsync(scopedTripRepo);
                case UserTripPacketType.InquireTrip:
                    return await HandleInquireTripAsync(packet.Trip);
                default:
                    // Unknown packet
                    logger.LogError("Unknown Packet Type: {Packet}", packet.ToJson());
                    return UserTripPacket.Error("Unknown packet type");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error while processing user packets");
            Console.WriteLine(ex.StackTrace);
            // If processing a single packet fails, send an Error packet back
            return UserTripPacket.Error("Error while processing user packets");
        }
    }
}
