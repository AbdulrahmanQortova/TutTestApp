using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tut.Common.Models;

namespace Tut.PageModels;

public partial class RideDetailsViewModel : ObservableObject
{
    public event EventHandler? RideRequested;
    public event EventHandler? RideCancelled;
    public event EventHandler? ChangeRidePressed;

    #region Properties
    [ObservableProperty]
    private bool _rideDetailsVisibility;

    [ObservableProperty]
    private bool _viewAfterDriverAcceptingVisibility;

    [ObservableProperty]
    private bool _findingDriverVisibility;

    [ObservableProperty]
    private bool _viewBeforeDriverAcceptingVisibility = true;

    [ObservableProperty]
    private ObservableCollection<Place> _tripPlaces = [];

    [ObservableProperty]
    private string _rideType = string.Empty;

    [ObservableProperty]
    private string _time = string.Empty;

    [ObservableProperty]
    private string _eta = string.Empty;

    [ObservableProperty]
    private string _paymentMethod = string.Empty;

    [ObservableProperty]
    private int _price;

    [ObservableProperty]
    private string _carImage = "car.png";

    [ObservableProperty]
    private string _driverName = "Karim Adel";

    [ObservableProperty]
    private string _carNumber = "ب ن ق 1234";

    [ObservableProperty]
    private string _pickupAddress = string.Empty;

    [ObservableProperty]
    private string _destinationAddress = string.Empty;

    [ObservableProperty]
    private Driver _driver = new();

    [ObservableProperty]
    private int _tripId;



    [ObservableProperty]
    private bool _isChatStarted;

    [ObservableProperty]
    private string _imageFromCamera = string.Empty;
    #endregion

    #region Commands
    public void RefreshViews()
    {
        ViewAfterDriverAcceptingVisibility = false;
        FindingDriverVisibility = false;
        ViewBeforeDriverAcceptingVisibility = false;
        RideDetailsVisibility = false;
    }

    [RelayCommand]
    private void ViewRideDetails()
    {
        RefreshViews();
        RideDetailsVisibility = true;
    }

    [RelayCommand]
    private void BackFromRideDetails()
    {
        RefreshViews();
        FindingDriverVisibility = true;
    }
    [RelayCommand]
    private async Task RequestRide()
    {
        try
        {
            OnRideRequested();
        }
        catch 
        {
        }
    }

    [RelayCommand]
    private async Task CancelRide()
    {
        OnRideCancelled();
        await Task.CompletedTask;
    }
    [RelayCommand]
    private async Task AddStops()
    {
        ChangeRidePressed?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }


    [RelayCommand]
    private async Task TakeSendPhoto()
    {
    }
    [RelayCommand]
    private async Task OpenChat()
    {
    }

    [RelayCommand]
    private async Task CallForHelp()
    {
    }
    [RelayCommand]
    private async Task ChatWithSupport()
    {
    }
    #endregion
    public RideDetailsViewModel()
    {
        IsChatStarted = false;
    }

    private void OnRideRequested()
    {
        RideRequested?.Invoke(this, EventArgs.Empty);
    }
    private void OnRideCancelled()
    {
        RideCancelled?.Invoke(this, EventArgs.Empty);
    }
}
