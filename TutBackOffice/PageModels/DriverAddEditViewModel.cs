using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tut.Common.Models;
namespace TutBackOffice.PageModels;

public partial class DriverAddEditViewModel(IPopupService popupService) : ObservableObject, IQueryAttributable
{
    [ObservableProperty]
    private string _firstName = string.Empty;
    [ObservableProperty]
    private string _lastName = string.Empty;
    [ObservableProperty]
    private string _mobile = string.Empty;
    [ObservableProperty]
    private string _password = string.Empty;
    [ObservableProperty]
    private string _email = string.Empty;
    [ObservableProperty]
    private string _nationalId = string.Empty;
    [ObservableProperty]
    private bool _isMobileReadOnly;

    private Driver _driver = new();
    
    [RelayCommand]
    private async Task Save()
    {
        _driver.Mobile = Mobile;
        _driver.FirstName = FirstName;
        _driver.LastName = LastName;
        _driver.Email = Email;
        _driver.NationalId = NationalId;
        _driver.Password = Password;

        await popupService.ClosePopupAsync(Shell.Current, _driver);
    }
    [RelayCommand]
    private async Task Cancel()
    {
        await popupService.ClosePopupAsync(Shell.Current);
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Driver", out object? d))
        {
            if (d is Driver driver)
            {
                _driver = driver;
                FirstName = driver.FirstName;
                LastName = driver.LastName;
                Mobile = driver.Mobile;
                Password = driver.Password;
                Email = driver.Email;
                NationalId = driver.NationalId;
                IsMobileReadOnly = true;
            }
        }
    }
}
