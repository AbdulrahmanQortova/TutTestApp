using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Diagnostics;
using Tut.Common.GServices;
using TutBackOffice.PageModels;
using TutBackOffice.Pages;

namespace TutBackOffice;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
        
        Mapsui.Logging.Logger.LogDelegate += (level, msg, ex) =>
        {
            Debug.WriteLine($"[Maps] {level}: {msg}");
            if(ex != null)
                Debug.WriteLine(ex.Message);
        };

#endif

//        builder.Services.AddSingleton<IGrpcChannelFactory>(new GrpcChannelFactory("http://qortova.com:8080"));
        builder.Services.AddSingleton<IGrpcChannelFactory>(new GrpcChannelFactory("http://localhost:5040"));
        
        
        builder.Services.AddTransientPopup<DriverAddEditPopup, DriverAddEditViewModel>();

        builder.Services.AddTransient<DriversManagementPageModel>();
        builder.Services.AddTransient<LiveTrackingPageModel>();

        return builder.Build();
    }
}
