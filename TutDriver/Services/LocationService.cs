namespace TutDriver.Services;

public partial class LocationService : ILocationService
{
    private Location? _lastLocation;
    public event EventHandler<GeolocationLocationChangedEventArgs>? LocationChanged;

    private partial Task SetupPlatformBackgroundLocation();

    public async Task SetupBackgroundLocation()
    {
        await SetupPlatformBackgroundLocation();
    }

    public async Task StartLocationUpdates()
    {
        try
        {
            Location? location = await Geolocation.Default.GetLastKnownLocationAsync();
            _lastLocation = location;
            Geolocation.Default.LocationChanged += OnLocationChanged;
            GeolocationListeningRequest request = new(GeolocationAccuracy.High);
            bool status = await Geolocation.Default.StartListeningForegroundAsync(request);
            System.Diagnostics.Debug.WriteLine(status
                ? "Started Listening to foreground location updates."
                : "Failed to start listening to foreground location updates.");
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"Start Listening to Location Updates failed: {e.Message}");
        }
    }

    public void StopLocationUpdates()
    {
        Geolocation.Default.LocationChanged -= OnLocationChanged;
        Geolocation.Default.StopListeningForeground();
    }

    public async Task<Location?> GetCurrentLocation()
    {
        return await Geolocation.Default.GetLocationAsync();
    }

    private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        _lastLocation = e.Location;
        LocationChanged?.Invoke(sender, e);
    }

    public async Task<PermissionStatus> RequestLocationPermissions()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status == PermissionStatus.Granted) return status;
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                // Handle permission denied
                System.Diagnostics.Debug.WriteLine("Location permission denied");
            }
            return status;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Location Permission request error: {ex.Message}");
        }
        return PermissionStatus.Denied;
    }
    public async Task<PermissionStatus> RequestLocationAlwaysPermissions()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
            if (status == PermissionStatus.Granted) return status;
            status = await Permissions.RequestAsync<Permissions.LocationAlways>();
            if (status != PermissionStatus.Granted)
            {
                // Handle permission denied
                System.Diagnostics.Debug.WriteLine("Location permission denied");
            }
            return status;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Location Permission request error: {ex.Message}");
        }
        return PermissionStatus.Denied;
    }

}