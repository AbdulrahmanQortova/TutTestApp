using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui;
using System.Collections.ObjectModel;
using Tut.Common.Models;

namespace TutMauiCommon.ViewModels;

public partial class QMapModel : ObservableObject
{
    public event EventHandler? Changed;

    [ObservableProperty] 
    private ObservableCollection<MapRoute> _routes = [];
    
    [ObservableProperty] 
    private ObservableCollection<MapCar> _cars = [];

    [ObservableProperty]
    private ObservableCollection<MapPoint> _endPoints = [];

    [ObservableProperty]
    private ObservableCollection<MapPoint> _stops = [];

    [ObservableProperty]
    private ObservableCollection<MapLine> _lines = [];

    [ObservableProperty]
    private MRect _extent = new(30.5, 30.5, 31, 31);

    public void ClearAll()
    {
        Routes.Clear();
        Cars.Clear();
        EndPoints.Clear();
        Stops.Clear();
        Lines.Clear();
        OnChanged();
    }

    public void OnChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public QMapModel()
    {
        EndPoints.CollectionChanged += (_,_) => CalculateExtent();
        Stops.CollectionChanged += (_,_) => CalculateExtent();
        Cars.CollectionChanged += (_,_) => CalculateExtent();
        Lines.CollectionChanged += (_,_) => CalculateExtent();
    }


    private IEnumerable<(double Lat, double Lon)> EnumerateLocations()
    {
        // Yield endpoints, stops and cars
        foreach (var e in EndPoints)
            yield return (e.Location.Latitude, e.Location.Longitude);

        foreach (var s in Stops)
            yield return (s.Location.Latitude, s.Location.Longitude);

        foreach (var c in Cars)
            yield return (c.Location.Latitude, c.Location.Longitude);

        // Lines: both start and end
        foreach (var line in Lines)
        {
            yield return (line.StartPoint.Latitude, line.StartPoint.Longitude);
            yield return (line.EndPoint.Latitude, line.EndPoint.Longitude);
        }
    }

    public void CalculateExtent()
    {
        // Collect all point coordinates (latitude, longitude)
        var points = EnumerateLocations()
            .Where(p => double.IsFinite(p.Lat) && double.IsFinite(p.Lon))
            .ToList();

        if (!points.Any())
            return;

        const double padding = 0.001;

        var minLat = points.Min(p => p.Lat) - padding;
        var maxLat = points.Max(p => p.Lat) + padding;
        var minLon = points.Min(p => p.Lon) - padding;
        var maxLon = points.Max(p => p.Lon) + padding;

        // Mapsui.MRect expects (minX, minY, maxX, maxY) where X=Longitude, Y=Latitude
        Extent = new MRect(minLon, minLat, maxLon, maxLat);
    }
    
    public class MapRoute
    {
        public Route Route { get; set; } = new();
        public Color Color { get; set; } = Colors.Black;
        public int Thickness { get; set; } = 2;
    }
    public class MapCar
    {
        public Location Location { get; set; } = new();
        public Color Color { get; set; } = Colors.Blue;
    }

    public class MapPoint
    {
        public Location Location { get; set; } = new();
        public Color Color { get; set; } = Colors.Green;
    }

    public class MapLine
    {
        public required Location StartPoint { get; set; }
        public required Location EndPoint { get; set; }
        public Color Color { get; set; } = Colors.Black;
        public int Thickness { get; set; } = 1;
    }
}
