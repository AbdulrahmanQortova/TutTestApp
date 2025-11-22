using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using Tut.Common.Business;
using Tut.PageModels.Popups;
using Tut.Popups;
using TutMauiCommon.Services;

namespace Tut;

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
                        Id = "TripChannel",
                        Name = "Trip Channel",
                        Description = "Trip Channel",
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
        
        builder.Services.AddSingleton<IShellService, ShellService>();
        builder.Services.AddSingleton<ILocationService, MockLocationService>();
        builder.Services.AddSingleton<IGeoService, MockGeoService>();


        builder.Services.AddTransientPopup<ArrivedPopup, ArrivedPopupModel>();
        return builder.Build();
    }
}
