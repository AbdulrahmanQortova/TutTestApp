using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.RegularExpressions;
namespace Tut.PageModels;

public partial class LoginPageModel : ObservableObject
{
    private readonly Regex NumsRegex = new Regex("^(01)([0-9]){9}$");

    #region Properties
    [ObservableProperty]
    private string mobileNumber = string.Empty; // Made observable for two-way binding and Clear()

    [ObservableProperty]
    private string password = string.Empty; // Made observable for two-way binding and Clear()

    [ObservableProperty]
    private string currentUserName = string.Empty; // For potential display or other uses, cleared by Clear()

    [ObservableProperty]
    bool loginFailed = true; // Remains as is
    #endregion


    #region Commands
    [RelayCommand]
    public async Task Login() // Changed to async Task
    {
        LoginFailed = true; // Reset login status
    }
    #endregion

    public void Clear()
    {
        MobileNumber = string.Empty;
        Password = string.Empty;
        CurrentUserName = string.Empty; // Clear the local UserName property
    }
}
