
using TutDriver.PageModels;
namespace TutDriver.Pages;

public partial class HomePage
{
    private readonly HomePageModel _pageModel;
    public HomePage(HomePageModel pageModel)
    {
        _pageModel = pageModel;
        InitializeComponent();
        BindingContext = _pageModel;


        NavigatedTo += async (_, _) => await _pageModel.StartAsync();
    }

}