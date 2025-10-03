using ProtoBuf.Grpc;
using Tut.Common.Models;
namespace Tut.Common.GServices;

public interface IGDriverTripService
{
    IAsyncEnumerable<DriverTripPacket> Connect(IAsyncEnumerable<DriverTripPacket> requestPackets, CallContext context = default);
}
