using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grpc.Net.Client;
using System.Collections.ObjectModel;
using Tut.Common.Models;
using ProtoBuf.Grpc.Client;
using System.Diagnostics;
using Tut.Common.GServices;
using TutBackOffice.Pages;
using CommunityToolkit.Maui.Extensions;
using Mapsui.Logging;
using Microsoft.Maui.Controls.Shapes;

namespace TutBackOffice.PageModels;

public partial class DriversManagementPageModel(IPopupService popupService) : ObservableObject
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


    [RelayCommand]
    private async Task ShowAdd()
    {
        await ShowAddEditPopup(null);
    }

    [RelayCommand]
    private async Task ShowEdit(Driver driver)
    {
        await ShowAddEditPopup(driver);
    }

    [RelayCommand]
    private async Task ShowAddEditPopup(Driver? driver)
    {
        try
        {
            Dictionary<string, object> queryAttributes = [];
            if(driver != null)
                queryAttributes.Add("Driver", driver);

            IPopupResult<Driver> result = await popupService.ShowPopupAsync<DriverAddEditViewModel, Driver>(
                Shell.Current,
                options:new PopupOptions
                {
                    CanBeDismissedByTappingOutsideOfPopup = false,
                    PageOverlayColor = Colors.WhiteSmoke.WithAlpha(0.2f),
                    Shape = null,
                },
                shellParameters:queryAttributes);


            Driver? resultDriver = result.Result;
            if (resultDriver is null)
            {
                // Cancelled - nothing to do
                return;
            }

            if (_driverManagerService == null)
            {
                Debug.WriteLine("Driver manager service not initialized");
                return;
            }

            // If Id == 0 assume new driver
            if (resultDriver.Id == 0)
            {
                try
                {
                    var addRes = await _driverManagerService.AddDriver(resultDriver);
                    // if backend returned an id, update local instance
                    if (addRes.Id != 0)
                    {
                        resultDriver.Id = addRes.Id;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
            else
            {
                try
                {
                    await _driverManagerService.UpdateDriver(resultDriver);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }

            // Refresh full list to keep UI in sync
            try
            {
                var drivers = await _driverManagerService.GetAllDrivers();
                Drivers = new ObservableCollection<Driver>(drivers);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
}
