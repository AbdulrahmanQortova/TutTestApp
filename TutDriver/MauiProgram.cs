using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using Tut.Common.GServices;
using Tut.Common.Managers;
using TutDriver.PageModels;
using TutDriver.Services;

namespace TutDriver;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseLocalNotification(config =>
            {
                config.AddAndroid(android =>
                {
                    android.AddChannel(new NotificationChannelRequest
                    {
                        Id = "ForegroundServiceChannel",
                        Name = "Foreground Service Channel",
                        Description = "Foreground Service Channel",
                        Importance = AndroidImportance.Low,
                    });
                    android.AddChannel(new NotificationChannelRequest
                    {
                        Id = "TripRequestChannel",
                        Name = "Trip Request Channel",
                        Description = "Trip Request Channel",
                        Importance = AndroidImportance.Default,
                    });
                });
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

//        builder.Services.AddSingleton<IGrpcChannelFactory>(new GrpcChannelFactory("http://qortova.com:8080"));
        builder.Services.AddSingleton<IGrpcChannelFactory>(new GrpcChannelFactory("http://localhost:5040"));
        
        builder.Services.AddSingleton<DriverLocationManagerService>();
        builder.Services.AddSingleton<ILocationService, LocationService>();



        builder.Services.AddTransient<HomePageModel>();
        
        return builder.Build();
    }
}
