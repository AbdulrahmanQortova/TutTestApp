using Mapsui;
using Mapsui.Projections;
using System.ComponentModel;
using Tut.PageModels;
using TutMauiCommon.Components;
namespace Tut.Pages;

public partial class PickOnMapPage : ContentPage
{
    private readonly PickOnMapViewModel _pageModel;
    private readonly QMap _map;

 
    public PickOnMapPage(PickOnMapViewModel pageModel)
    {
        InitializeComponent();
        _pageModel = pageModel;
        BindingContext = _pageModel;

        _map = new QMap();
        MapControl.Map = _map;
        _map.SetModel(_pageModel.MapModel);
        
        _map.Navigator.ViewportChanged += Navigator_ViewportChanged;
        
        NavigatedTo += async (_, _) =>
        {
            _pageModel.Reset();
            await _pageModel.OnNavigatedTo();
            await UpdateInitialLocation();
        };
    }
    
    private void Navigator_ViewportChanged(object? sender, PropertyChangedEventArgs e)
    {
        Navigator navigator = _map.Navigator;
        Viewport mapCentre = navigator.Viewport; 
        (double longitude, double latitude) = SphericalMercator.ToLonLat(mapCentre.CenterX, mapCentre.CenterY);

        if (double.IsNaN(latitude) || double.IsNaN(longitude)) return;

        _pageModel.SelectedLatitude = latitude;
        _pageModel.SelectedLongitude = longitude;
        _pageModel.PlaceName = $"{latitude:F6} - {longitude:F6}"; // Format for display
    }
    
    private async Task UpdateInitialLocation()
    {
        var currentLocation = new Location
        {
            Latitude = 30.006747,
            Longitude = 31.424302
        };
        if (currentLocation != null)
        {
            _pageModel.SelectedLatitude = currentLocation.Latitude;
            _pageModel.SelectedLongitude = currentLocation.Longitude;
            _pageModel.PlaceName = $"{currentLocation.Latitude:F6} - {currentLocation.Longitude:F6}";
            (double x, double y) = SphericalMercator.FromLonLat(_pageModel.SelectedLongitude, _pageModel.SelectedLatitude);
 
            double desiredWidthInMeters = 5000;
            var viewport = _map.Navigator.Viewport;
            double resolution = desiredWidthInMeters / viewport.Width;
            _map.Navigator.CenterOnAndZoomTo(new MPoint(x,y), resolution);
        }
    }


}