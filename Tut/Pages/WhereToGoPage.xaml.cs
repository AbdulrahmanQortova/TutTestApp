
using Tut.PageModels;
namespace Tut.Pages;

public partial class WhereToGoPage : ContentPage
{

    public WhereToGoPage(WhereToGoPageModel pageModel)
    {
        InitializeComponent();
        BindingContext = pageModel;

        NavigatedTo += async (s, e) =>
        {
            await pageModel.InitializeAsync();
        };
    }
}
