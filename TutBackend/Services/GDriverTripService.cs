using Grpc.Core;
using ProtoBuf.Grpc;
using System.Threading.Channels;
using Tut.Common.GServices;
using Tut.Common.Models;
using TutBackend.Repositories;
namespace TutBackend.Services;

public class GDriverTripService(
    IDriverRepository driverRepository,
    QipClient qipClient) 
    : IGDriverTripService
{
    private Driver? _driver;

    private Channel<DriverTripPacket>? _responseChannel;
    
    public async IAsyncEnumerable<DriverTripPacket> Connect(IAsyncEnumerable<DriverTripPacket> requestPackets, CallContext context = default)
    {
        _driver = await AuthUtils.AuthorizeDriver(context, driverRepository, qipClient);
        if (_driver is null)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthorized"));

        _responseChannel = Channel.CreateBounded<DriverTripPacket>(new BoundedChannelOptions(20)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        
        
        
        yield break;
    }
}
