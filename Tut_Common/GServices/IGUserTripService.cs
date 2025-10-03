using Tut.Common.Models;
namespace Tut.Common.GServices;

public interface IGUserTripService
{
    public IAsyncEnumerable<UserTripPacket> Connect(IAsyncEnumerable<UserTripPacket> requestPackets);
    public void ProvideFeedback(Feedback feedback);
}
