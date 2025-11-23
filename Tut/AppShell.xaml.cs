using Tut.Pages;
namespace Tut;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterPages();
    }
    private void RegisterPages()
    {
        Routing.RegisterRoute(nameof(PickOnMapPage), typeof(PickOnMapPage));
        Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
        Routing.RegisterRoute(nameof(TripPage), typeof(TripPage));
        Routing.RegisterRoute(nameof(MyTripsPage), typeof(MyTripsPage));
        Routing.RegisterRoute(nameof(WhereToGoPage), typeof(WhereToGoPage));
        Routing.RegisterRoute(nameof(SetLocationPage), typeof(SetLocationPage));
        Routing.RegisterRoute(nameof(RatingPage), typeof(RatingPage));
    }
}
