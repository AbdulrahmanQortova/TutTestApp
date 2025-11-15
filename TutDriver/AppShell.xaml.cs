using TutDriver.Pages;

namespace TutDriver;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        Routing.RegisterRoute(nameof(TripPage), typeof(TripPage));
    }
}
