using TutBackOffice.PageModels;

namespace TutBackOffice.Pages;

public partial class DriversManagementPage : ContentPage
{
    public DriversManagementPage(DriversManagementPageModel model)
    {
        InitializeComponent();
        BindingContext = model;

        NavigatedTo += (_, _) =>
        {
            model.Start();
        };

        NavigatedFrom += (_, _) =>
        {
            model.Stop();
        };
    }
}
