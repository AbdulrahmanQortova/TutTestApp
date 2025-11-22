using Tut.PageModels;

namespace Tut.Pages;
public partial class HomePage
{
    public HomePage(HomePageModel pageModel)
    {
        InitializeComponent();
        BindingContext = pageModel;
    }
}
