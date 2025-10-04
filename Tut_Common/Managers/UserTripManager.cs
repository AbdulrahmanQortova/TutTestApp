using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;
using System.Threading.Channels;
using Tut.Common.GServices;
using Tut.Common.Models;
namespace Tut.Common.Managers;

public class UserTripManager
{
    private IGUserTripService _userTripService;
    private Channel<UserTripPacket>? _requestChannel;
    
    public event EventHandler<StatusUpdateEventArgs>? StatusChanged;
    public event EventHandler<ErrorReceivedEventArgs>? ErrorReceived; 
    public event EventHandler<NotificationReceivedEventArgs>? NotificationReceived;
    public event EventHandler<DriverLocationsReceivedEventARg>? DriverLocationsReceived; 
    
    public UserTripManager(
        IGrpcChannelFactory channelFactory
        )
    {
        GrpcChannel grpcChannel = channelFactory.GetChannel();
        _userTripService = grpcChannel.CreateGrpcService<IGUserTripService>();
        

    }


    public async Task Connect(CancellationToken cancellationToken)
    {
        _requestChannel = Channel.CreateBounded<UserTripPacket>(new BoundedChannelOptions(20)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
        
        
    }




}


public class StatusUpdateEventArgs : EventArgs
{
    public Trip? Trip { get; set; }
}
public class ErrorReceivedEventArgs : EventArgs
{
    public string ErrorText { get; set; } = string.Empty;
}
public class NotificationReceivedEventArgs : EventArgs
{
    public string NotificationText { get; set; } = string.Empty;
}
public class DriverLocationsReceivedEventARg : EventArgs
{
    public List<GLocation> Locations { get; set; } = [];
}
