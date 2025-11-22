namespace TutMauiCommon.Services;

public class MockLocationService : ILocationService
{
    private readonly Location _start = new()
    {
        Latitude = 30.0974,
        Longitude = 31.3736
    };

    private readonly Location _end = new()
    {
        Latitude = 30.00585,
        Longitude = 31.22983
    };

    private Location _current = new()
    {
        Latitude = 30.0974,
        Longitude = 31.3736
    };

    private readonly int _secondsToComplete = 1500;
    private readonly int _msecBeforeFirstLocation = 5000;
    
    
    private bool _sendingUpdates;
    private bool _firstUpdate;
    
    
    
    
    
    public MockLocationService()
    {
        Task.Run(Loop);
    }

    private void Loop()
    {
        int curSecond = 0;
        bool forth = true;
        while (true)
        {
            while (curSecond < _secondsToComplete)
            {
                if (forth)
                {
                    _current.Latitude = (_end.Latitude - _start.Latitude) * curSecond / _secondsToComplete + _start.Latitude;
                    _current.Longitude = (_end.Longitude - _start.Longitude) * curSecond / _secondsToComplete + _start.Longitude;
                }
                else
                {
                    _current.Latitude = (_start.Latitude - _end.Latitude) * curSecond / _secondsToComplete + _end.Latitude;
                    _current.Longitude = (_start.Longitude - _end.Longitude) * curSecond / _secondsToComplete + _end.Longitude;
                }

                if (_sendingUpdates)
                {
                    LocationChanged?.Invoke(this, new GeolocationLocationChangedEventArgs(_current));
                }
                
                Thread.Sleep(1000);
                curSecond++;
            }
            forth = !forth;
            curSecond = 0;
        }
    }
    
    public event EventHandler<GeolocationLocationChangedEventArgs>? LocationChanged;
    public Task SetupBackgroundLocation()
    {
        return Task.CompletedTask;
    }

    public async Task StartLocationUpdates()
    {
        if (_firstUpdate)
        {
            await Task.Delay(_msecBeforeFirstLocation);
            _firstUpdate = false;
        }

        _sendingUpdates = true;
    }

    public void StopLocationUpdates()
    {
        _sendingUpdates = false;
    }

    public async Task<Location?> GetCurrentLocation()
    {
        if (_firstUpdate)
        {
            await Task.Delay(_msecBeforeFirstLocation);
            _firstUpdate = false;
        }


        return _current;
    }
    public Task<PermissionStatus> RequestLocationPermissions()
    {
        return Task.FromResult(PermissionStatus.Granted);
    }
    public Task<PermissionStatus> RequestLocationAlwaysPermissions()
    {
        return Task.FromResult(PermissionStatus.Granted);
    }
}