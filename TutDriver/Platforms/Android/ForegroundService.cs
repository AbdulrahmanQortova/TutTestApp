using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using _Microsoft.Android.Resource.Designer;

namespace TutDriver;

[Service(Name = "TutDriver.ForegroundService", Exported = true)]
public class ForegroundService : Service
{
    private const int ServiceRunningNotificationId = 10000;
    private const string ServiceChannelId = "ForegroundServiceChannel";
    private bool _isRunning;

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (_isRunning)
            return StartCommandResult.Sticky;

        _isRunning = true;
        StartForeground(ServiceRunningNotificationId, CreateNotification("Monitoring current location"));
        return StartCommandResult.Sticky;
    }




    private Notification CreateNotification(string message)
    {
        var notificationIntent = new Intent(this, typeof(MainActivity));
        var pendingIntent = PendingIntent.GetActivity(this, 0, notificationIntent, PendingIntentFlags.Immutable);
                
        var notification = new NotificationCompat.Builder(this, ServiceChannelId)
            .SetContentTitle("Pegasus Driver App")!
            .SetContentText(message)!
            .SetSmallIcon(ResourceConstant.Mipmap.appicon)!
            .SetOngoing(true)!
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(message))!
            .SetContentIntent(pendingIntent)!
            .Build();
        return notification!;
    }
        
    public override void OnDestroy()
    {
        _isRunning = false;
        base.OnDestroy();
    }

}