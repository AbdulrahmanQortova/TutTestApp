using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Tut.Common.GServices;
using Tut.Common.Models;
using TutMauiCommon.ViewModels;


namespace TutBackOffice.PageModels;

public record DriverFilterRow(int Id, string Name, bool Selected);

public partial class LiveTrackingPageModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DriverFilterRow> _driversList = [];
    
    [ObservableProperty]
    private QMapModel _mapModel = new();
    
    
    private readonly IGDriverLocationService _driverLocationService;
    private readonly IGTripManagerService _tripManagerService;

    private CancellationTokenSource? _cts;

    
    public LiveTrackingPageModel(IGrpcChannelFactory channelFactory)
    {
        GrpcChannel channel = channelFactory.GetChannel();
        _driverLocationService = channel.CreateGrpcService<IGDriverLocationService>();
        _tripManagerService = channel.CreateGrpcService<IGTripManagerService>();

    }
    
    
    public void Start()
    {
        if (_cts != null) return;
        
        
        
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => RunLoop(_cts));
    }

    public void Stop()
    {
        if (_cts == null) return;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    private async Task RunLoop(CancellationTokenSource cts)
    {
        while (!cts.IsCancellationRequested)
        {
            try
            {
                await Iteration();
                if(cts.IsCancellationRequested) return;
                await Task.Delay(5000);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
    }

    private async Task Iteration()
    {
        List<Trip> activeTrips = await _tripManagerService.GetAllActiveTrips(new GPartialListRequest());
        List<DriverLocation> driverLocations = await _driverLocationService.GetDriverLocations();

        List<QMapModel.MapRoute> routes = [];
        List<QMapModel.MapPoint> endPoints = [];
        List<QMapModel.MapPoint> stops = [];
        List<QMapModel.MapCar> cars = [];
        
        foreach (Trip trip in activeTrips)
        {
            if (trip.Stops.Count < 2)
                continue;
            endPoints.Add(new QMapModel.MapPoint()
            {
                Location = new Location(trip.Stops[0].Place!.Location.Latitude, trip.Stops[0].Place!.Location.Longitude),
                Color = Colors.Green
            });
            endPoints.Add(new QMapModel.MapPoint()
            {
                Location = new Location(trip.Stops[^1].Place!.Location.Latitude, trip.Stops[^1].Place!.Location.Longitude),
                Color = Colors.GreenYellow
            });
            for (int i = 1; i < trip.Stops.Count -1; i++)
            {
                if (trip.Stops.Count < i) 
                    break;
                stops.Add(new QMapModel.MapPoint()
                {
                    Location = new Location(trip.Stops[i].Place!.Location.Latitude, trip.Stops[i].Place!.Location.Longitude),
                    Color = Colors.Yellow
                });
            }
            routes.Add(new QMapModel.MapRoute
            {
                Route = new Route(trip.Route)
            });
        }

        cars.AddRange(driverLocations.Select(driverLocation => new QMapModel.MapCar
        {
            Location = new Location(driverLocation.Location.Latitude, driverLocation.Location.Longitude),
        }));

        QMapModel mapModel = new()
        {
            Routes = new ObservableCollection<QMapModel.MapRoute>(routes),
            EndPoints = new ObservableCollection<QMapModel.MapPoint>(endPoints),
            Stops = new ObservableCollection<QMapModel.MapPoint>(stops),
            Cars = new ObservableCollection<QMapModel.MapCar>(cars)
        };
        mapModel.CalculateExtent();
        
        MapModel = mapModel;
    }

}

