using Grpc.Core;
using ProtoBuf.Grpc;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Tut.Common.GServices;
using Tut.Common.Models;
using Tut.Common.Utils;
using TutBackend.Repositories;
namespace TutBackend.Services;

public class GUserTripService(
    IUserRepository userRepository,
    ITripRepository tripRepository,
    QipClient qipClient,
    IServiceScopeFactory scopeFactory,
    ILogger<GDriverManagerService> logger
    )
: IGUserTripService
{

    private Channel<UserTripPacket>? _responseChannel;
    private User? _user;
    private readonly CancellationTokenSource _cancellation = new ();
    private Trip? _activeTrip;

    public async IAsyncEnumerable<UserTripPacket> Connect(IAsyncEnumerable<UserTripPacket> requestPackets, CallContext context = default)
    {
        User? user = await AuthUtils.AuthorizeUser(context, userRepository, qipClient);
        _user = user ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthorized"));


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
                    UserTripPacket res = await DispatchIncomingPacket(packet, scopeFactory, _cancellation.Token);
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

        _ = Task.Run(() => ReportTripStatusUpdates(scopeFactory, _cancellation.Token), CancellationToken.None);

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
    public Task<UserTripPacket> GetState()
    {
        return Task.FromResult(UserTripPacket.StatusUpdate(_activeTrip));
    }


    private async Task ReportTripStatusUpdates(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
    {
        TripState lastCommunicatedState = TripState.Unspecified;
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
            if (_user is null) continue;
            if (_responseChannel is null) continue;

            try
            {
                using var scope = scopeFactory.CreateScope();
                var scopedTripRepo = scope.ServiceProvider.GetRequiredService<ITripRepository>();

                _activeTrip = await scopedTripRepo.GetActiveTripForUser(_user.Id);
                if (_activeTrip is null)
                {
                    if(lastCommunicatedState == TripState.Unspecified) continue;
                    await _responseChannel.Writer.WriteAsync(UserTripPacket.StatusUpdate(_activeTrip), cancellationToken);
                    _activeTrip = null;
                    lastCommunicatedState = TripState.Unspecified;
                    break;
                }

                if (_activeTrip.Status == lastCommunicatedState) continue;

                await _responseChannel.Writer.WriteAsync(UserTripPacket.StatusUpdate(_activeTrip), cancellationToken);
                lastCommunicatedState = _activeTrip.Status;
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

    private async Task<UserTripPacket> HandleRequestTripAsync(ITripRepository scopedTripRepo, IUserRepository scopedUserRepo, UserTripPacket packet)
    {
        if (packet.Trip is null) return new UserTripPacket();
        _activeTrip = packet.Trip;
        User? user = await scopedUserRepo.GetByIdAsync(_user!.Id);
        _activeTrip.User = user!;
        Trip? prevTrip = await tripRepository.GetActiveTripForUser(user!.Id);
        if (prevTrip is not null)
            return UserTripPacket.Error("User already has an active trip");
        await scopedTripRepo.AddAsync(_activeTrip);
        return new UserTripPacket();
    }
    
    private async Task<UserTripPacket> HandleCancelTripAsync(ITripRepository scopedTripRepo, UserTripPacket packet)
    {
        if (_user is null) return new UserTripPacket();
        if (_activeTrip is null)
        {
            logger.LogError("User {User} Cancelled Trip while there are no active trip for him", _user.FullName);
            return new UserTripPacket();
        }

        _activeTrip.Status = TripState.Canceled;
        _activeTrip.CancelTime = DateTime.UtcNow;
        await scopedTripRepo.UpdateAsync(_activeTrip);
        return UserTripPacket.StatusUpdate(_activeTrip);
    }
    
    private async Task<UserTripPacket> DispatchIncomingPacket(UserTripPacket packet, IServiceScopeFactory scopeFactory, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var scopedTripRepo = scope.ServiceProvider.GetRequiredService<ITripRepository>();
            var scopedUserRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            switch (packet.Type)
            {
                case UserTripPacketType.GetStatus:
                    return UserTripPacket.StatusUpdate(_activeTrip);
                case UserTripPacketType.RequestTrip:
                    return await HandleRequestTripAsync(scopedTripRepo, scopedUserRepo, packet);
                case UserTripPacketType.CancelTrip:
                    return await HandleCancelTripAsync(scopedTripRepo, packet);
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
