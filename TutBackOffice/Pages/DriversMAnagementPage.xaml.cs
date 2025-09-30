using TutBackOffice.PageModels;

namespace TutBackOffice.Pages;

public partial class DriversManagementPage : ContentPage
{
    private readonly DriversManagementPageModel _model = new();
    public DriversManagementPage()
    {
        InitializeComponent();
        BindingContext = _model;

        NavigatedTo += (_, _) =>
        {
            _model.Start();
        };

        NavigatedFrom += (_, _) =>
        {
            _model.Stop();
        };
    }
}
