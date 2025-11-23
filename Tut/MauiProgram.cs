using CommunityToolkit.Maui;
using Grpc.Net.Client.Balancer;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Tut.Common.Business;
using Tut.Common.GServices;
using Tut.Common.Managers;
using Tut.Common.Mocks;
using Tut.PageModels;
using Tut.PageModels.Popups;
using Tut.Pages;
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



        // android.runtime.JavaProxyThrowable
        // This registers all required handlers for SKGLView and resolves the crash.
        builder.UseSkiaSharp();



#if DEBUG
        builder.Logging.AddDebug();
#endif
        
//            builder.Services.AddSingleton<IGrpcChannelFactory>(new GrpcChannelFactory("http://192.168.1.21:5002"));
        builder.Services.AddSingleton<IGrpcChannelFactory>(new GrpcChannelFactory("http://qortova.com:8080"));

        
        builder.Services.AddSingleton<IShellService, ShellService>();
        builder.Services.AddSingleton<ILocationService, MockLocationService>();
        builder.Services.AddSingleton<IGeoService, MockGeoService>();
        builder.Services.AddSingleton<IUserTripManager, MockUserTripManager>();


        builder.Services.AddTransientPopup<ArrivedPopup, ArrivedPopupModel>();


        #region DI Containers (ViewModles)
        builder.Services.AddTransient<HomePageModel>();
        builder.Services.AddTransient<TripPageModel>();
        builder.Services.AddTransient<RideDetailsViewModel>();
        builder.Services.AddTransient<LoginPageModel>();
        builder.Services.AddTransient<MyTripsPageModel>();
        builder.Services.AddTransient<PickOnMapViewModel>();
        builder.Services.AddTransient<RatingPageModel>();
        builder.Services.AddTransient<SetLocationPageModel>();
        builder.Services.AddTransient<WhereToGoPageModel>();
        #endregion




        return builder.Build();
    }
}
