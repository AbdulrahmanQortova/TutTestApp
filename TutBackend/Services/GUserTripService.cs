using Grpc.Core;
using ProtoBuf.Grpc;
using System.Threading.Channels;
using Tut.Common.GServices;
using Tut.Common.Models;
using Tut.Common.Utils;
using TutBackend.Repositories;
namespace TutBackend.Services;

public class GUserTripService(
    IUserRepository userRepository,
    QipClient qipClient,
    IServiceScopeFactory scopeFactory,
    ILogger<GDriverManagerService> logger
    )
    : IGUserTripService
{

    private Channel<UserTripPacket>? _responseChannel;
    // store ids only to avoid holding entity instances across threads
    private int _userId = -1; // -1 indicates no authenticated user
    // no lingering entity references; we fetch fresh entities from scoped repos
    private readonly CancellationTokenSource _cancellation = new ();

    public async IAsyncEnumerable<UserTripPacket> Connect(IAsyncEnumerable<UserTripPacket> requestPackets, CallContext context = default)
    {
        User? user = await AuthUtils.AuthorizeUser(context, userRepository, qipClient);
        var resolvedUser = user ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthorized"));
        _userId = resolvedUser.Id;


        _responseChannel = Channel.CreateBounded<UserTripPacket>(new BoundedChannelOptions(20)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        // Background task: consume incoming request packets and act on them.
        _ = Task.Run(() => ProcessIncomingRequestsAsync(requestPackets), CancellationToken.None);
        _ = Task.Run(() => ReportTripStatusUpdates(_cancellation.Token), CancellationToken.None);

        // Subscribe to response channel and yield packets as they become available
        var reader = _responseChannel.Reader;
        try
        {
            while (await reader.WaitToReadAsync(_cancellation.Token))
            {
                while (reader.TryRead(out var outPacket))
                {
                    if(outPacket.Type != UserTripPacketType.Unspecified)
                        yield return outPacket;
                }
            }
        }
        finally
        {
            // ensure channel is completed
            _responseChannel.Writer.TryComplete();
        }

    }

    private async Task ProcessIncomingRequestsAsync(IAsyncEnumerable<UserTripPacket> requestPackets)
    {
        try
        {
            await foreach (var packet in requestPackets.WithCancellation(_cancellation.Token))
            {
                UserTripPacket res = await DispatchIncomingPacket(packet);
                if(_responseChannel != null)
                    await _responseChannel.Writer.WriteAsync(res, _cancellation.Token);
            }
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
        {
            // expected cancellation
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while ProcessingIncomingRequests in UserTripService");
        }
        await _cancellation.CancelAsync();
        _cancellation.Dispose();
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
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(10, cancellationToken);
            if (_userId == -1) continue;
            if (_responseChannel is null) continue;

            try
            {
                using var scope = scopeFactory.CreateScope();
                var scopedTripRepo = scope.ServiceProvider.GetRequiredService<ITripRepository>();
                lastCommunicatedState = await ProcessAndWriteTripStatusAsync(scopedTripRepo, lastCommunicatedState, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // expected
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while reporting trip status updates");
            }
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

    private async Task<UserTripPacket> HandleRequestTripAsync(ITripRepository scopedTripRepo, Trip? trip)
    {
        if (trip is null) return UserTripPacket.Error("Trip Requested without specifying the trip");
        // Use fresh user entity from scoped repo and avoid storing trip entity globally
        Trip? prevTrip = await scopedTripRepo.GetActiveTripForUser(_userId);
        if (prevTrip is not null)
            return UserTripPacket.Error("User already has an active trip");
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
    
    private async Task<UserTripPacket> DispatchIncomingPacket(UserTripPacket packet)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var scopedTripRepo = scope.ServiceProvider.GetRequiredService<ITripRepository>();

            switch (packet.Type)
            {
                case UserTripPacketType.GetStatus:
                    // Resolve a fresh trip for status
                    if (_userId == -1) return UserTripPacket.Error("Unauthorized");
                    Trip? trip = await scopedTripRepo.GetActiveTripForUser(_userId);
                    return UserTripPacket.StatusUpdate(trip);
                case UserTripPacketType.RequestTrip:
                    return await HandleRequestTripAsync(scopedTripRepo, packet.Trip);
                case UserTripPacketType.CancelTrip:
                    return await HandleCancelTripAsync(scopedTripRepo);
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
