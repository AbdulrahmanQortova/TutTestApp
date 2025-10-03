using Grpc.Net.Client;

namespace Tut.Common.GServices;

public interface IGrpcChannelFactory
{
    GrpcChannel GetChannel();
    GrpcChannel GetChannel(string address);
    GrpcChannel GetNewChannel();
    GrpcChannel GetNewChannel(string address);
}