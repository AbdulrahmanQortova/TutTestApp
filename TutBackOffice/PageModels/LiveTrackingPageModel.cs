using CommunityToolkit.Mvvm.ComponentModel;

using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Tut.Common.GServices;
using Tut.Common.Models;
using TutMauiCommon.ViewModels;


namespace TutBackOffice.PageModels;

public record DriverFilterRow
{
    public string Name { get; init; } = string.Empty;
    public bool Selected { get; set; }

    public DriverFilterRow() { }
    public DriverFilterRow(string name, bool selected = false)
    {
        Name = name;
        Selected = selected;
    }
}

public partial class LiveTrackingPageModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DriverFilterRow> _driversList = [];
    
    [ObservableProperty]
    private QMapModel _mapModel = new();
    
    
    private readonly IGDriverLocationService _driverLocationService;
    private readonly IGTripManagerService _tripManagerService;
    private readonly IGDriverManagerService _driverManagerService;

    private CancellationTokenSource? _cts;

    
    public LiveTrackingPageModel(IGrpcChannelFactory channelFactory)
    {
        GrpcChannel channel = channelFactory.GetChannel();
        _driverLocationService = channel.CreateGrpcService<IGDriverLocationService>();
        _tripManagerService = channel.CreateGrpcService<IGTripManagerService>();
        _driverManagerService = channel.CreateGrpcService<IGDriverManagerService>();

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
        // Fetch all drivers to determine their current state so we can filter out offline/unspecified
        List<Driver> allDrivers = await _driverManagerService.GetAllDrivers();
        var driverStateById = allDrivers.ToDictionary(d => d.Id, d => d.State);

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

        // Only add cars for drivers that exist and whose state is neither Offline nor Unspecified
        cars.AddRange(driverLocations
            .Where(dl => driverStateById.TryGetValue(dl.DriverId, out var state) && state != DriverState.Offline && state != DriverState.Unspecified)
            .Select(driverLocation => new QMapModel.MapCar
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
