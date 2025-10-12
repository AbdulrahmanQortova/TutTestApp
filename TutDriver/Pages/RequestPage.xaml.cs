using Android.Gms.Common.Util;
using TutDriver.Services;
namespace TutDriver.Pages;

public partial class RequestPage
{
    private readonly RequestPageViewModel _pm;

    public RequestPage()
	{
		InitializeComponent();
        _pm = ServiceHelper.GetService<RequestPageViewModel>()!;
        BindingContext = _pm;

        CreateTheMap();
    }
    private void CreateTheMap()
    {
        Mapsui.Map map = MapUtils.CreateMap();
        mapView.Map = map;
        Location? location = ServiceHelper.GetService<ILocationService>()!.GetCurrentLocation();
        MapUtils.UpdateMapLocation(mapView, location!.Latitude, location.Longitude);

        _pm.Received();
    }

    public void Received(object sender, EventArgs e) { _pm.Received(); }

    public async Task StartTrip(object sender, EventArgs e)
    {
        await _pm.StartTrip();
    }

    public async Task Arrived(object sender, EventArgs e)
    {
        await _pm.Arrived();
    }

    public async Task EndTrip(object sender, EventArgs e)
    {
        await _pm.EndTrip();
    }
    public async Task PaymentDone(object sender, EventArgs e) 
    { 
        await _pm.PaymentDone();
        await Navigation.PopAsync();
    }
}