
using Tut.PageModels;
namespace Tut.Pages;

public partial class MyTripsPage : ContentPage
{
    public MyTripsPage(MyTripsPageModel pageModel)
    {
        InitializeComponent();

        BindingContext = pageModel;
        
        NavigatedTo += async (_, _) => await pageModel.NavigatedToAsync();
    }
}