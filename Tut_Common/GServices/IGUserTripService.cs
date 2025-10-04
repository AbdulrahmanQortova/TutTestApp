using ProtoBuf.Grpc;
using Tut.Common.Models;
namespace Tut.Common.GServices;

public interface IGUserTripService
{
    public IAsyncEnumerable<UserTripPacket> Connect(IAsyncEnumerable<UserTripPacket> requestPackets, CallContext context = default);
    public void ProvideFeedback(Feedback feedback);
}
