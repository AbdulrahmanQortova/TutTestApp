using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui;
using System.Collections.ObjectModel;
using Tut.Common.Models;

namespace TutMauiCommon.ViewModels;

public partial class QMapModel : ObservableObject
{
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
    private MRect _extent = new(29, 29, 31, 31);


    public QMapModel()
    {
        EndPoints.CollectionChanged += (_,_) => CalculateExtent();
        Stops.CollectionChanged += (_,_) => CalculateExtent();
        Cars.CollectionChanged += (_,_) => CalculateExtent();
        Lines.CollectionChanged += (_,_) => CalculateExtent();
    }


    private void CalculateExtent()
    {
        
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
        public Location StartPoint { get; set; } = new();
        public Location EndPoint { get; set; } = new();
        public Color Color { get; set; } = Colors.Yellow;
        public int Thickness { get; set; } = 1;
    }
}

