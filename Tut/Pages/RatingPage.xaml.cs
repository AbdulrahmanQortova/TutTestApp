
using Tut.PageModels;
namespace Tut.Pages;

public partial class RatingPage : ContentPage
{
	public RatingPage()
	{
		InitializeComponent();
        BindingContext = Application.Current?.Handler?.MauiContext?.Services?.GetService<RatingPageModel>();
    }
    protected override bool OnBackButtonPressed()
    {
        return true;
    }
}
