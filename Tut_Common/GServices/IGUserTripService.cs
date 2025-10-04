using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using Tut.Common.Models;
namespace Tut.Common.GServices;

[Service]
public interface IGUserTripService
{
    public IAsyncEnumerable<UserTripPacket> Connect(IAsyncEnumerable<UserTripPacket> requestPackets, CallContext context = default);
    public void ProvideFeedback(Feedback feedback);

    public Task<UserTripPacket> GetState();
}
