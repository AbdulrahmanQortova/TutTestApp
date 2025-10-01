using CommunityToolkit.Maui.Views;
using Tut.Common.Models;
using TutBackOffice.PageModels;
namespace TutBackOffice.Pages;

public partial class DriverAddEditPopup : Popup<Driver>
{
    public DriverAddEditPopup(DriverAddEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
