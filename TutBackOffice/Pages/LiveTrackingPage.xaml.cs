using CommunityToolkit.Mvvm.Input;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.UI.Maui;
using Mapsui.Widgets.ButtonWidgets;
using System.ComponentModel;
using TutBackOffice.PageModels;
using TutMauiCommon.Components;
using VerticalAlignment = Mapsui.Widgets.VerticalAlignment;


namespace TutBackOffice.Pages;

public partial class LiveTrackingPage
{

    private readonly LiveTrackingPageModel _pageModel;
    private readonly QMap _map;

    public LiveTrackingPage(LiveTrackingPageModel pageModel)
    {
        InitializeComponent();

        _pageModel = pageModel;
        BindingContext = _pageModel;

        _map = new QMap();
        MapControl.Map = _map;
        _map.Widgets.Add(new ZoomInOutWidget
        {
            VerticalAlignment = VerticalAlignment.Top
        });
        _map.SetModel(_pageModel.MapModel);
        
        NavigatedTo += async (sender, args) =>
        {
            await Start();
        };

        NavigatedFrom += async (sender, args) =>
        {
            await Stop();
        };
        
        _pageModel.PropertyChanged += OnPageModelPropertyChanged;

    }
    
    private void OnPageModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LiveTrackingPageModel.MapModel):
                _map.SetModel(_pageModel.MapModel);
                break;
        }
    }
    
    private Task Start()
    {
        _pageModel.Start();
        return Task.CompletedTask;
    }

    private Task Stop()
    {
        _pageModel.Stop();
        return Task.CompletedTask;
    }



    
    private void Recenter(object? sender, EventArgs e)
    {
        _map.Recenter();
    }

    private void SwitchDriverListVisibility(object? sender, EventArgs e)
    {
        DriverList.IsVisible = !DriverList.IsVisible;
    }




}

