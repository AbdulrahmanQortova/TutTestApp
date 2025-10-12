using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.LocalNotification;
using Tut.Common.Managers;
using Tut.Common.Models;
using TutDriver.Services;
namespace TutDriver.PageModels;

public partial class HomePageModel(
    DriverLocationManagerService driverLocationManagerService,
    DriverTripManager driverTripManager,
    ILocationService locationService,
    INotificationService notificationService
    ) : ObservableObject
{
    [ObservableProperty]
    private string _fullName = string.Empty;
    [ObservableProperty]
    private bool _isPunchedIn;
    [ObservableProperty]
    private bool _isOffline;
    [ObservableProperty]
    private bool _isStartTripVisible;

    private bool _isInitialized;

    
    public async Task StartAsync()
    {
        driverTripManager.OfferReceived += HandleOfferReceived;
        driverTripManager.ConnectionStateChanged += HandleConnectionStateChanged;
        if (!_isInitialized) await InitializeAsync();
    }

    public Task StopAsync()
    {
        driverTripManager.OfferReceived -= HandleOfferReceived;
        return Task.CompletedTask;
    }

    private async Task InitializeAsync()
    {
        _isInitialized = true;
        bool notificationEnabled = await notificationService.AreNotificationsEnabled();
        if (!notificationEnabled)
        {
            await notificationService.RequestNotificationPermission();
            notificationEnabled = await notificationService.AreNotificationsEnabled();
        }
        if (!notificationEnabled)
            await Shell.Current.DisplayAlert("Permission Error", "Notification Permission MUST be allowed for Tut Driver App to run.", "Ok");

        if (DeviceInfo.Platform == DevicePlatform.iOS)
        {
            PermissionStatus status = await locationService.RequestLocationAlwaysPermissions();
            if (status != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert("Permission Error", "Location Permission MUST be allowed for Tut Driver App to run.", "Ok");
            }
        }
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            PermissionStatus status = await locationService.RequestLocationPermissions();
            if (status != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert("Permission Error", "Location Permission MUST be allowed for Tut Driver App to run.", "Ok");
            }
        }
        
        await locationService.SetupBackgroundLocation();
        locationService.LocationChanged += (_, e) =>
        {
            notificationService.Show(new NotificationRequest
            {
                NotificationId = 10000,
                Title = "Tut Driver",
                Description = $"Updated: {DateTime.Now:T}\nLatitude: {e.Location.Latitude}\nLongitude: {e.Location.Longitude}",
                Android =
                {
                    ChannelId = "ForegroundServiceChannel"
                }
            });
            driverLocationManagerService.RegisterLocation(new GLocation
            {
                Latitude = e.Location.Latitude,
                Longitude = e.Location.Longitude
            });
        };
        await locationService.StartLocationUpdates();
        
        driverLocationManagerService.SetAccessToken("DA10");
        driverLocationManagerService.ErrorReceived += (_, e) => Shell.Current.DisplayAlert("Error", "LocationManager Error: " + e.ErrorText, "Ok");
        await driverLocationManagerService.Connect(CancellationToken.None);
        
        driverTripManager.SetAccessToken("DA10");
        driverTripManager.ErrorReceived += (_, e) => Shell.Current.DisplayAlert("Error", "DriverTripManager Error: " + e.ErrorText, "Ok");
        await driverTripManager.Connect(CancellationToken.None);
    }
    
    [RelayCommand]
    private async Task AcceptTripAsync()
    {
        
    }

    [RelayCommand]
    private async Task PunchInAsync()
    {
        await driverTripManager.SendPunchInAsync();
    }

    [RelayCommand]
    private async Task PunchOutAsync()
    {
        await driverTripManager.SendPunchOutAsync();
    }

    private void HandleOfferReceived(object? s, StatusUpdateEventArgs e)
    {
        IsStartTripVisible = true;
        notificationService.Show(new NotificationRequest
            {
                NotificationId = 10000,
                Title = "Tut Driver",
                Description = "يوجد طلب وارد",
                Android =
                {
                    ChannelId = "TripRequestChannel"
                }
            }
        );
    }
    private async void HandleConnectionStateChanged(object? s, ConnectionStateChangedEventArgs e)
    {
        IsOffline = e.NewState != ConnectionState.Connected;
    }
}
