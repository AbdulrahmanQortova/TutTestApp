
using System.Collections.ObjectModel;
using Tut.Common.Models;
using Tut.PageModels;
namespace Tut.Pages;
public partial class SetLocationPage
{
    public SetLocationPage(SetLocationPageModel pageModel)
    {
        InitializeComponent();
        
        // First initialize the ViewModel
        BindingContext = pageModel;
        

        NavigatedTo += async (_, _) =>
        {
            await pageModel.OnNavigatedTo();
        };
    }
    
    private async void BackButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch
        {
            // prevent exceptions from propagating out of async void
        }
    }
}
