using CommunityToolkit.Mvvm.ComponentModel;
using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Tut.Common.Dto.MapDtos;
using Tut.Common.GServices;


namespace TutBackOffice.PageModels;

public record DriverFilterRow(int Id, string Name, bool Selected);

public partial class LiveTrackingPageModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DriverFilterRow> _driversList = [];
    
    [ObservableProperty]
    private ObservableCollection<Location> _visibleDriverLocations = [];
    
    [ObservableProperty]
    private ObservableCollection<RouteDto>
    
    
    private List<Location> _driverLocations = [];
    

    private IGDriverManagerService? _driverManagerService;
    private IGDriverLocationService? _driverLocationService;
    private IGTripManagerService? _tripManagerService;

    private CancellationTokenSource? _cts;
    
    public void Start()
    {
        if (_cts != null) return;
        
        GrpcClientFactory.AllowUnencryptedHttp2 = true;
        GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:5040");
        
        _driverManagerService = channel.CreateGrpcService<IGDriverManagerService>();       
        _driverLocationService = channel.CreateGrpcService<IGDriverLocationService>();
        _tripManagerService = channel.CreateGrpcService<IGTripManagerService>();
        
        _cts = new CancellationTokenSource();
    }

    public void Stop()
    {
        if (_cts == null) return;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }
    
    
    public async Task Initialize()
    {
    }

}

