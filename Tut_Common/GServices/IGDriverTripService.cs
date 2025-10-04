using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using Tut.Common.Models;
namespace Tut.Common.GServices;

[Service]
public interface IGDriverTripService
{
    IAsyncEnumerable<DriverTripPacket> Connect(IAsyncEnumerable<DriverTripPacket> requestPackets, CallContext context = default);
}
