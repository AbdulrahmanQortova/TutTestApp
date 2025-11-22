using Tut.PageModels;

namespace Tut.Pages;

public partial class TripPage
{


    public TripPage(TripPageModel tripPageModel)
    {
        InitializeComponent();
        MyRideDetails.BindingContext = tripPageModel.RideDetailsVm;
        BindingContext = tripPageModel;
        tripPageModel.AnimatedImage = AnimationImage;
        NavigatedTo += async(s, e) => await tripPageModel.OnNavigatedTo();
    }


   

}
