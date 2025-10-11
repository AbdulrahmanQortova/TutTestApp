namespace TutDriver.Services;

public partial class LocationService
{
    private partial async Task SetupPlatformBackgroundLocation()
    {
        // Start the foreground service
        await MainActivity.Instance!.StartForegroundService();
    }


}