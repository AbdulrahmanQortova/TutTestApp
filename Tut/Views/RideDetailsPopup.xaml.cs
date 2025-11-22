
namespace Tut.Views;

public partial class RideDetailsPopup
{
	public RideDetailsPopup()
	{
		InitializeComponent();
    }
    private void HideShowContent(object sender, TappedEventArgs e)
    {
        if (RideDetailsContent.IsVisible)
        {
            RideDetailsContent.IsVisible = false;
            MinimumHeightRequest = 50;
        }
        else
        {
            RideDetailsContent.IsVisible = true;
            MinimumHeightRequest = 300;
        }

    }

}
