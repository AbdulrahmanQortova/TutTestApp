using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace TutDriver;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    public static MainActivity? Instance { get; private set; }
    private Intent? _foregroundServiceIntent;

    public MainActivity()
    {
        Instance = this;
    }
    public Task StartForegroundService()
    {
        _foregroundServiceIntent = new Intent(this, typeof(ForegroundService));
        StartForegroundService(_foregroundServiceIntent);
        return Task.CompletedTask;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_foregroundServiceIntent != null)
        {
            StopService(_foregroundServiceIntent);
        }
    }

}
