using Tut.PageModels;
using TutMauiCommon.Components;

namespace Tut.Pages;

public partial class TripPage
{
    public TripPage(TripPageModel tripPageModel)
    {
        InitializeComponent();
        MyRideDetails.BindingContext = tripPageModel.RideDetailsVm;
        BindingContext = tripPageModel;

        // Wire up the map with QMapModel
        var map = new QMap();
        MapControl.Map = map;
        map.SetModel(tripPageModel.MapModel);
        
        NavigatedTo += async (_, _) => await tripPageModel.OnNavigatedTo();
        NavigatedFrom += async (_, _) => await tripPageModel.OnDisappearing();
    }
}

