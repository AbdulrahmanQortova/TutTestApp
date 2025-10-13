using Mapsui.Extensions;
using Mapsui.Widgets.ButtonWidgets;
using System.ComponentModel;
using TutDriver.PageModels;
using TutMauiCommon.Components;
using VerticalAlignment = Mapsui.Widgets.VerticalAlignment;

namespace TutDriver.Pages;

public partial class TripPage
{
    private readonly TripPageModel _pageModel;
    private readonly QMap _map;
    public TripPage(TripPageModel pageModel)
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
        
        NavigatedTo += async (_, _) =>
        {
            await Start();
        };
        
        NavigatedFrom += async (_, _) =>
        {
            await Stop();
        };
        
        _pageModel.PropertyChanged += OnPageModelPropertyChanged;
        
    }
    
    
    private async Task Start()
    {
        await _pageModel.StartAsync();
    }

    private async Task Stop()
    {
        await _pageModel.StopAsync();
    }


    private void OnPageModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(TripPageModel.MapModel):
                _map.SetModel(_pageModel.MapModel);
                break;
        }
    }

}