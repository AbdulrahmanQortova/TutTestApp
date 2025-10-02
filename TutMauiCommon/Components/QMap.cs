using BruTile.Cache;
using BruTile.Predefined;
using Mapsui;
using Mapsui.Nts;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using NetTopologySuite.Geometries;
using System.Collections.ObjectModel;
using System.ComponentModel;
using TutMauiCommon.ViewModels;

using Color = Microsoft.Maui.Graphics.Color;
using Easing = Mapsui.Animations.Easing;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace TutMauiCommon.Components;

public class QMap : Mapsui.Map
{
    private QMapModel? _model;
    private readonly ObservableCollection<GeometryFeature> _routeFeatures = [];
    private readonly ObservableCollection<GeometryFeature> _carFeatures = [];
    private readonly ObservableCollection<GeometryFeature> _shapeFeatures = [];
    private Mapsui.Layers.MemoryLayer RoutesLayer { get; }
    private Mapsui.Layers.MemoryLayer CarsLayer { get; }
    private Mapsui.Layers.MemoryLayer ShapesLayer { get; }

    private DateTime _lastProgrammaticViewportChange;
    private bool _shouldAutoUpdateViewport = true;
    public QMap()
    {
        string cacheDir = Path.Combine(FileSystem.AppDataDirectory, "tut_tiles");
        if (!Directory.Exists(cacheDir))
            Directory.CreateDirectory(cacheDir);

        var fileCache = new FileCache(cacheDir, "png");
        Layers.Add(new TileLayer(KnownTileSources.Create(KnownTileSource.OpenStreetMap, null, fileCache)));
        RoutesLayer = new Mapsui.Layers.ObservableMemoryLayer<GeometryFeature>((i) => i, "Routes")
        {
            Enabled = true,
            ObservableCollection = _routeFeatures
        };
        Layers.Add(RoutesLayer);

        CarsLayer = new Mapsui.Layers.ObservableMemoryLayer<GeometryFeature>(i => i, "Cars")
        {
            Enabled = true,
            ObservableCollection = _carFeatures
        };
        Layers.Add(CarsLayer);
        
        ShapesLayer = new Mapsui.Layers.ObservableMemoryLayer<GeometryFeature>(i => i, "Shapes")
        {
            Enabled = true,
            ObservableCollection = _shapeFeatures
        };
        Layers.Add(ShapesLayer);

        _lastProgrammaticViewportChange = DateTime.Now;
        Navigator.ViewportChanged += OnViewportChanged;
    }

    private void OnViewportChanged(object? sender, ViewportChangedEventArgs e)
    {
        Console.WriteLine("OnViewportChanged");
        DateTime n = DateTime.Now;
        DateTime lst = _lastProgrammaticViewportChange;
        TimeSpan ts = n - lst;
        if (ts < TimeSpan.FromMilliseconds(4000))
            return;
        Console.WriteLine($"Now: {n}, Last: {lst}");
        _shouldAutoUpdateViewport = false;
    }
    public void ShowRouteLayer(bool show)
    {
        RoutesLayer.Enabled = show;
        RefreshData();
    }

    public void ShowCarsLayer(bool show)
    {
        CarsLayer.Enabled = show;
        RefreshData();
    }

    public void ShowShapesLayer(bool show)
    {
        ShapesLayer.Enabled = show;
        RefreshData();
    }


    public void SetModel(QMapModel model)
    {
        if (_model != null)
        {
            _model.PropertyChanged -= OnModelPropertyChanged;
        }
        _model = model;
        Redraw();
        _model.PropertyChanged += OnModelPropertyChanged;
    }

    public void Redraw()
    {
        DrawModelCars();
        DrawModelRoutes();
        DrawModelShapes();
        ViewportChanged();
    }

    public void Recenter()
    {
        _shouldAutoUpdateViewport = true;
        ViewportChanged();
    }
    
    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(QMapModel.Routes):
            case nameof(QMapModel.EndPoints):
            case nameof(QMapModel.Stops):
                DrawModelRoutes();
                break;
            case nameof(QMapModel.Cars):
                DrawModelCars();
                break;
            case nameof(QMapModel.Lines):
                DrawModelShapes();
                break;
            case nameof(QMapModel.Extent):
                ViewportChanged();
                break;
        }
    }

    private void DrawModelRoutes()
    {
        if (_model == null) return;
        _routeFeatures.Clear();
        foreach (QMapModel.MapRoute route in _model.Routes)
        {
            DrawRoute(route);
        }
        foreach (QMapModel.MapPoint endpoint in _model.EndPoints)
        {
            _routeFeatures.Add(CreatePointFeature(endpoint.Location.Latitude, endpoint.Location.Longitude, MapsColor(endpoint.Color)));
        }
        foreach (QMapModel.MapPoint endpoint in _model.Stops)
        {
            _routeFeatures.Add(CreatePointFeature(endpoint.Location.Latitude, endpoint.Location.Longitude, MapsColor(endpoint.Color)));
        }
        RefreshData();
    }

    private void DrawModelCars()
    {
        if (_model == null) return;
        _carFeatures.Clear();
        foreach (QMapModel.MapCar car in _model.Cars)
        {
            _carFeatures.Add(CreateCarFeature(car.Location.Latitude, car.Location.Longitude, MapsColor(car.Color)));
        }
        RefreshData();
    }

    private void DrawModelShapes()
    {
        if (_model == null) return;
        _shapeFeatures.Clear();
        foreach (QMapModel.MapLine line in _model.Lines)
        {
            _carFeatures.Add(CreateLineFeature(Project(line.StartPoint), Project(line.EndPoint), MapsColor(line.Color), line.Thickness));
        }
        RefreshData();
    }
    

    private void DrawRoute(QMapModel.MapRoute route)
    {
        if (route.Route.Points.Count == 0)
            return;
        Coordinate[] routePoints = new Coordinate[route.Route.Points.Count];
        for (int i = 0; i < route.Route.Points.Count; i++)
        {
            routePoints[i] = Project(route.Route.Points[i].Lng, route.Route.Points[i].Lat);
        }
        // Use the new helper to create the route feature
        _routeFeatures.Add(CreateRouteFeature(routePoints, MapsColor(route.Color), route.Thickness));
    }

    // Create a route feature from projected coordinates and a Mapsui color
    private static GeometryFeature CreateRouteFeature(Coordinate[] routePoints, Mapsui.Styles.Color color, int thickness)
    {
        return new GeometryFeature
        {
            Geometry = new LineString(routePoints),
            Styles = new List<IStyle>
            {
                new VectorStyle
                {
                    Line = new Pen(color)
                    {
                        PenStrokeCap = PenStrokeCap.Round,
                        StrokeJoin = StrokeJoin.Bevel,
                        Width = thickness
                    },
                }
            }
        };
    }

    private static GeometryFeature CreateLineFeature(Coordinate startPoint, Coordinate endPoint, Mapsui.Styles.Color color, int thickness)
    {
        return new GeometryFeature
        {
            Geometry = new LineString([startPoint, endPoint]),
            Styles = new List<IStyle>
            {
                new VectorStyle
                {
                    Line = new Pen(color)
                    {
                        PenStrokeCap = PenStrokeCap.Round,
                        StrokeJoin = StrokeJoin.Bevel,
                        Width = thickness
                    }
                }
            }
        };
    }


    private static GeometryFeature CreateCarFeature(double lat, double lon, Mapsui.Styles.Color color)
    {
        GeometryFeature car = new GeometryFeature
        {
            Geometry = new NetTopologySuite.Geometries.Point(Project(lon, lat)),
            Styles = new List<IStyle>
            {
                new SymbolStyle
                {
                    Fill = new Mapsui.Styles.Brush(color),
                    Outline = new Pen(color),
                    SymbolScale = 0.5,
                    Opacity = 0.8f
                }
            },
        };
        return car;
    }


    private static GeometryFeature CreatePointFeature(double lat, double lon, Mapsui.Styles.Color color)
    {
        GeometryFeature point = new GeometryFeature
        {
            Geometry = new NetTopologySuite.Geometries.Point(Project(lon, lat)),
            Styles = new List<IStyle>
            {
                new SymbolStyle
                {
                    Fill = new Mapsui.Styles.Brush(color),
                    Outline = new Pen(color),
                    SymbolScale = 0.5,
                    Opacity = 0.8f
                }
            },
        };
        return point;
    }

    public void Clear()
    {
        _routeFeatures.Clear();
        _carFeatures.Clear();
        _shapeFeatures.Clear();
    }

    private void ViewportChanged()
    {
        if (_model is null) return;
        if (!_shouldAutoUpdateViewport) return;
        
        Coordinate minCoord = Project(_model.Extent.MinX, _model.Extent.MinY);
        Coordinate maxCoord = Project(_model.Extent.MaxX, _model.Extent.MaxY);
        MRect box = new MRect(minCoord.X, minCoord.Y, maxCoord.X, maxCoord.Y);
        _lastProgrammaticViewportChange = DateTime.Now;
        Console.WriteLine($"Setting Last Change to now: {_lastProgrammaticViewportChange}");
        Navigator.ZoomToBox(box, MBoxFit.Fit, 200, Easing.CubicInOut);
    }

    private static Mapsui.Styles.Color MapsColor(Color color)
    {
        Mapsui.Styles.Color c = new Mapsui.Styles.Color((int)(color.Red * 255), (int)(color.Green * 255), (int)(color.Blue * 255), (int)(color.Alpha * 255));
        return c;
    }
    private static Coordinate Project(double lon, double lat)
    {
        return SphericalMercator.FromLonLat(lon, lat).ToCoordinate();
    }

    private static Coordinate Project(Location location)
    {
        return SphericalMercator.FromLonLat(location.Longitude, location.Latitude).ToCoordinate();
    }

}
