namespace TutMauiCommon.Services;

public interface ILocationService
{
    public event EventHandler<GeolocationLocationChangedEventArgs> LocationChanged;

    Task SetupBackgroundLocation();
    Task StartLocationUpdates();
    void StopLocationUpdates();
    Task<Location?> GetCurrentLocation();
    Task<PermissionStatus> RequestLocationPermissions();
    Task<PermissionStatus> RequestLocationAlwaysPermissions();
}