using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;

namespace Tut.Common.GServices;

public class GrpcChannelFactory : IGrpcChannelFactory
{
    private readonly GrpcChannel _channel;
    private readonly string _defaultAddress;

    public GrpcChannelFactory(string defaultAddress)
    {
        _defaultAddress = defaultAddress;
        GrpcClientFactory.AllowUnencryptedHttp2 = true;
        _channel = GrpcChannel.ForAddress(defaultAddress);
    }

    public GrpcChannel GetChannel()
    {
        return _channel;
    }

    public GrpcChannel GetChannel(string address)
    {
        return address == _defaultAddress ? _channel : GrpcChannel.ForAddress(address);
    }
}