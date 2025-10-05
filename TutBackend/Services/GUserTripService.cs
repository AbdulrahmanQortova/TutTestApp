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
    private readonly CancellationTokenSource _cancellation = new ();
    // no lingering entity references; we fetch fresh entities from scoped repos
    private int _activeTripId = -1; // -1 indicates no active trip tracked

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
        _ = Task.Run(async () =>
         {
             try
             {
                 await foreach (var packet in requestPackets.WithCancellation(_cancellation.Token))
                 {
                    UserTripPacket res = await DispatchIncomingPacket(packet);
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
        Trip? trip;
        if (_activeTripId != -1)
        {
            trip = await scopedTripRepo.GetByIdAsync(_activeTripId);
        }
        else
        {
            trip = await scopedTripRepo.GetActiveTripForUser(_userId);
        }
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
                lastCommunicatedState = await ProcessAndWriteTripStatusAsync(scopedTripRepo, cancellationToken, lastCommunicatedState);
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
    private async Task<TripState> ProcessAndWriteTripStatusAsync(ITripRepository scopedTripRepo, CancellationToken cancellationToken, TripState lastCommunicatedState)
    {
        var currentTrip = await scopedTripRepo.GetActiveTripForUser(_userId);

        if (currentTrip is not null)
        {
            if (currentTrip.Status != lastCommunicatedState)
            {
                await _responseChannel!.Writer.WriteAsync(UserTripPacket.StatusUpdate(currentTrip), cancellationToken);
                lastCommunicatedState = currentTrip.Status;
            }
            _activeTripId = currentTrip.Id;
            return lastCommunicatedState;
        }

        // no active trip
        if (lastCommunicatedState == TripState.Unspecified) return lastCommunicatedState;
        await _responseChannel!.Writer.WriteAsync(UserTripPacket.StatusUpdate(null), cancellationToken);
        _activeTripId = -1;
        return TripState.Unspecified;
    }

    private async Task<UserTripPacket> HandleRequestTripAsync(ITripRepository scopedTripRepo, IUserRepository scopedUserRepo, UserTripPacket packet)
    {
        if (packet.Trip is null) return new UserTripPacket();
        // Use fresh user entity from scoped repo and avoid storing trip entity globally
        User? user = await scopedUserRepo.GetByIdAsync(_userId);
        packet.Trip.User = user!;
        Trip? prevTrip = await scopedTripRepo.GetActiveTripForUser(user!.Id);
        if (prevTrip is not null)
            return UserTripPacket.Error("User already has an active trip");
        var tripToAdd = packet.Trip;
        await scopedTripRepo.AddAsync(tripToAdd);
        _activeTripId = tripToAdd.Id;
        return new UserTripPacket();
    }
    
    private async Task<UserTripPacket> HandleCancelTripAsync(ITripRepository scopedTripRepo)
    {
        if (_userId == -1) return new UserTripPacket();

        var currentTrip = await scopedTripRepo.GetActiveTripForUser(_userId);
        if (currentTrip is null)
        {
            // try to get user full name for logging if possible
            try
            {
                using var scope = scopeFactory.CreateScope();
                var scopedUserRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var u = await scopedUserRepo.GetByIdAsync(_userId);
                logger.LogError("User {User} Cancelled Trip while there are no active trip for him", u?.FullName ?? _userId.ToString());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "User {UserId} Cancelled Trip while there are no active trip for him", _userId);
            }
            return new UserTripPacket();
        }

        currentTrip.Status = TripState.Canceled;
        currentTrip.CancelTime = DateTime.UtcNow;
        await scopedTripRepo.UpdateAsync(currentTrip);
        _activeTripId = -1;
        return UserTripPacket.StatusUpdate(currentTrip);
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
                    Trip? trip;
                    if (_activeTripId != -1)
                    {
                        trip = await scopedTripRepo.GetByIdAsync(_activeTripId);
                    }
                    else
                    {
                        trip = await scopedTripRepo.GetActiveTripForUser(_userId);
                    }
                    return UserTripPacket.StatusUpdate(trip);
                case UserTripPacketType.RequestTrip:
                    return await HandleRequestTripAsync(scopedTripRepo, scopedUserRepo, packet);
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
