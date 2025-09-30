using CommunityToolkit.Mvvm.ComponentModel;
using Grpc.Net.Client;
using System.Collections.ObjectModel;
using Tut.Common.Models;
using ProtoBuf.Grpc.Client;
using System.Diagnostics;
using Tut.Common.GServices;

namespace TutBackOffice.PageModels;

public partial class DriversManagementPageModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Driver> _drivers = [];

    private IGDriverManagerService? _driverManagerService;

    private CancellationTokenSource? _cts;
    public void Start()
    {
        if (_cts != null) return;
        
        GrpcClientFactory.AllowUnencryptedHttp2 = true;
        GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:5040");
        
        _driverManagerService = channel.CreateGrpcService<IGDriverManagerService>();        
        
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => RefreshLoop(_cts.Token));
    }

    public void Stop()
    {
        if (_cts == null) return;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    private async Task RefreshLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_driverManagerService != null)
            {
                try
                {
                    List<Driver> drivers = await _driverManagerService.GetAllDrivers();
                    Drivers = new ObservableCollection<Driver>(drivers);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
            await Task.Delay(5000, token);
        }
        _driverManagerService = null;
    }
    
    /*
    
    [RelayCommand]
    public async Task ShowEdit(DriverType driver)
    {
       
        if (driver != null)
        {
            SelectedDriver = driver;
            await PopupNavigation.Instance.PushAsync(new EditDriverPopup(driver));
        }
    }
    [RelayCommand]
    public async Task ShowAdd()
    {
        SelectedDriver = new DriverType();
        await PopupNavigation.Instance.PushAsync(new AddDriverPopup());
    }
    [RelayCommand]
    public async Task EditDriver(DriverType driver)
    {
     
        if (driver != null)
        {
            await _driverService.UpdateDriver(driver);
            await GetAllDrivers();
            await PopupNavigation.Instance.PopAsync();
            SelectedDriver = new DriverType();
        }
    }
    [RelayCommand]
    public async Task DeleteDriver(DriverType driver)
    {
        bool answer = await Shell.Current.DisplayAlert("Delete", $"Are you sure you want to delete {driver.Username}?", "Yes", "No");
        if (answer)
        {
            string errorStr = (await _driverService.DeleteDriver(driver.Id)).ErrorMsg;
            if (string.IsNullOrEmpty(errorStr))
            {
                Alldrivers.Remove(driver);
                await GetAllDrivers();
                SelectedDriver = new DriverType();
            }
            else
            {
                await ShowAlert(errorStr);
            }
        }
    }
    [RelayCommand]
    public async Task AddNewDriver(DriverType driver)
    {
        if (driver != null)
        {
            int dnum = Alldrivers?.Count ?? 0;
            driver.NationalId = $"{"D"+dnum}";
            driver.Image = "D2";
            string Result=   await _driverService.AddDriver(driver);
            if (!string.IsNullOrEmpty(Result)) 
            {
                await PopupNavigation.Instance.PushAsync(new ToastPopup()
                {
                    Message = $"{Result}"
                });
                return;
            }
            await GetAllDrivers();
            SelectedDriver = new DriverType();
            await PopupNavigation.Instance.PopAsync();
        }
    }

    [RelayCommand]
    private void DriverRoute(DriverType driver)
    {
        Shell.Current.GoToAsync(nameof(DriverHistoryPage), new ShellNavigationQueryParameters
        {
            ["Driver"] = driver
        });
    }
    */
}
