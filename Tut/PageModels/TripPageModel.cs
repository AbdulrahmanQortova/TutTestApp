using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using Tut.Common.Business;
using Tut.Common.Dto.MapDtos;
using Tut.Common.Managers;
using Tut.Common.Models;
using Tut.Common.Utils;
using Tut.PageModels.Popups;
using Tut.Pages;
using Tut.Popups;
using Tut.Utils;
using TutMauiCommon.ViewModels;

namespace Tut.PageModels;

public partial class TripPageModel : ObservableObject, IQueryAttributable
{
    private readonly UserTripManager _tripManager;
    private readonly INotificationService _notificationService;
    private readonly IPopupService _popupService;
    private readonly IGeoService _geoService;
    
    private readonly QMapModel _qMapModel = new();

    [ObservableProperty]
    private List<Place> _tripPlaces = [];

    [ObservableProperty]
    private Trip? _trip;

    public Image? AnimatedImage { get; set; }

    /*--Estimated strings are now stored on RideDetailsVm; remove local duplicates to avoid redundancy--*/
    [ObservableProperty]
    private RideDetailsViewModel _rideDetailsVm;

    [ObservableProperty]
    private QMapModel _mapModel = new();

    // UI State Properties
    [ObservableProperty]
    private bool _isAnimationVisible;


    public TripPageModel(
        IGeoService geoService,
        IPopupService popupService,
        INotificationService notificationService,
        UserTripManager tripManager,
        RideDetailsViewModel rideDetailsVm)
    {
        _tripManager = tripManager;
        _notificationService = notificationService;
        _popupService = popupService;
        _geoService = geoService;
        
        RideDetailsVm = rideDetailsVm;
        rideDetailsVm.RideRequested += async (_,_) => await RideDetailsVm_RideRequested();
        rideDetailsVm.RideCancelled += async (_,_) => await RideDetailsVm_RideCancelled();
        rideDetailsVm.ChangeRidePressed += RideDetailsVm_ChangeRidePressed;
    }

    public async Task OnNavigatedTo()
    {
        ShowBeforeDriverAcceptance();
        await ConfirmDestination();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("TripPlaces", out object? tripPlacesObject)) return;
        TripPlaces = tripPlacesObject as List<Place> ?? [];
        Trip = new Trip
        {
            User = new User
            {
                Id = 1,
                Mobile = "01023490066",
                FirstName = "Ualed",
                LastName = "Uawas"
            },
            Stops = TripPlaces,
        };
    }

    public void Reset()
    {
        _qMapModel.ClearAll();
    }

    private Task CancelRide()
    {
        return Task.CompletedTask;
    }


    #region Trip Lifecycle & Events
    private void ShowBeforeDriverAcceptance()
    {
        _tripManager.DriverLocationsReceived += UpdateDriverLocation;
        _tripManager.StatusChanged += OnTripManagerStateChanged;
        SetBeforeRequestUiState();
    }


    private async void OnTripManagerStateChanged(object? sender, EventArgs e)
    {
        if (_tripManager.CurrentTrip is null) return;
        try
        {
            switch (_tripManager.CurrentTrip.Status)
            {
                case TripState.Requested:
                    SetFindingDriverUiState();
                    break;
                case TripState.Accepted:
                    SetWaitingDriverUiState();
                    GetDriverData();
                    break;
                case TripState.DriverArrived:
                    await _notificationService.Show(new NotificationRequest
                    {
                        NotificationId = 1001,
                        Title = "Tut",
                        Description = "Your driver has arrived.",
                        ReturningData = "DriverArrived", // Returning data when tapped on notification.
                        Android =
                        {
                            IconLargeName = new AndroidIcon("drivericon.png"),
                            IconSmallName = new AndroidIcon("drivericon.png"),
                            Priority = AndroidPriority.High,
                        }
                    });
                    await _popupService.ShowPopupAsync<ArrivedPopup>(Shell.Current,
                        options: PopupOptions.Empty,
                        shellParameters: new Dictionary<string, object>
                    {
                        { nameof(ArrivedPopupModel.Icon) , "drivericon.png"},
                        { nameof(ArrivedPopupModel.Title) , "Your driver has arrived."},
                        {  nameof(ArrivedPopupModel.Message), "Please meet the captain \nat the pickup point."},
                    });
                    break;
                case TripState.AtStop:
                case TripState.Ongoing:
                    await _popupService.ClosePopupAsync(Shell.Current);
                    SetOnTripUiState();
                    break;
                case TripState.Arrived:
                    RideDetailsVm.RefreshViews();
                    RideDetailsVm.ViewAfterDriverAcceptingVisibility = true;
                    await _popupService.ShowPopupAsync<ArrivedPopup>(Shell.Current,
                        options: PopupOptions.Empty,
                        shellParameters: new Dictionary<string, object>
                    {
                        { nameof(ArrivedPopupModel.Icon) , "money.png"},
                        {nameof(ArrivedPopupModel.Title) , "You've arrived."},
                        { nameof(ArrivedPopupModel.Message) , "Please proceed to pay the fare."},
                        { nameof(ArrivedPopupModel.Money), $"{RideDetailsVm.Price:F0}" },
                        { nameof(ArrivedPopupModel.Currency), "LE"},
                    });
                    break;
                case TripState.Ended:
                {
                    await _popupService.ClosePopupAsync(Shell.Current);
                    RideDetailsVm.ViewAfterDriverAcceptingVisibility = false;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Shell.Current.GoToAsync(nameof(RatingPage), new ShellNavigationQueryParameters
                        {
                            { StringConstants.TripId, _tripManager.CurrentTrip.Id }
                        });
                    });
                    break;
                }
            }
        }
        catch
        {
            // Prevent exceptions from propagating out of async void
        }
    }

    private async Task RideDetailsVm_RideRequested()
    {
        await RequestRideAsync();
    }

    private async Task RideDetailsVm_RideCancelled()
    {
        try
        {
            await CancelRide();
        }
        catch
        {
            // Prevent Exceptions from propagating out of async void
        }
    }

    private void RideDetailsVm_ChangeRidePressed(object? sender, EventArgs e)
    {
        // Implement Change Ride Logic.
    }
    #endregion

    #region Trip Actions & Helpers
    private async Task ConfirmDestination()
    {

        DirectionResponseDto? directionResponseDto =
            await _geoService.GetRouteDataAsync(ApplicationProperties.GoogleApiKey,
                TripPlaces[0].ToLocation(),
                TripPlaces[^1].ToLocation(),
                TripPlaces.Skip(1).Take(TripPlaces.Count - 2)
                    .Where(p => p is { Latitude: > 0, Longitude: > 0 })
                    .Select(p => p.ToLocation())
                    .ToList());

        if (directionResponseDto != null)
        {
            RideDetailsVm.PickupAddress = TripPlaces[0].Address;
            RideDetailsVm.DestinationAddress = TripPlaces[^1].Address;

            double distance = MapDtoUtils.GetRouteDistance(directionResponseDto.Routes![0]);
            double time = MapDtoUtils.GetRouteTime(directionResponseDto.Routes![0]);
            RideDetailsVm.Price = 55;
            RideDetailsVm.Eta = $"{(int)(time / 60)} min away";
            RideDetailsVm.Time = (DateTime.Now.AddMinutes((int)(time / 60))).ToString("hh:mm tt");
        }
    }

    private async Task InquireRideAsync()
    {
        if (Trip == null) return;
        await _tripManager.SendInquireTripAsync(Trip);
    }
    
    private async Task RequestRideAsync()
    {
        if (Trip == null) return;
        _ = Task.Run(StartAnimation);

        IsAnimationVisible = true; // Use local property

        await _tripManager.SendRequestTripAsync(Trip);
    }

    private void GetDriverData()
    {
        if (_tripManager.CurrentTrip?.Driver == null) return;
        RideDetailsVm.Driver = _tripManager.CurrentTrip.Driver;
        RideDetailsVm.DriverName = RideDetailsVm.Driver.FullName;
        RideDetailsVm.CarNumber = "abc 123";
    }
    #endregion

    #region UI State
    private void SetBeforeRequestUiState()
    {
        RefreshView();
        // UI-specific visibility properties removed from HomeViewModel; RideDetailsVm controls ride-details UI
        RideDetailsVm.RefreshViews();
        RideDetailsVm.ViewBeforeDriverAcceptingVisibility = true;
    }

    private void SetFindingDriverUiState()
    {
        RefreshView();
        // No local IsRequestRideVisible property anymore
        RideDetailsVm.RefreshViews();
        RideDetailsVm.FindingDriverVisibility = true;
        _ = Task.Run(StartAnimation);
    }

    private void SetWaitingDriverUiState()
    {
        RefreshView();
        RideDetailsVm.RefreshViews();
        RideDetailsVm.ViewAfterDriverAcceptingVisibility = true;
    }

    private static void SetOnTripUiState()
    {
        // No relevant UI changes for this state
    }

    private void UpdateDriverLocation(object? sender, DriverLocationsReceivedEventArgs e)
    {
        List<QMapModel.MapCar> cars = e.Locations.Select(loc => new QMapModel.MapCar
        {
            Location = new Location(loc.Latitude, loc.Longitude),
            Color = Colors.Red
        }).ToList();
        QMapModel newModel = new QMapModel
        {
            EndPoints = MapModel.EndPoints,
            Routes = MapModel.Routes,
            Cars = cars.ToObservableCollection(),
            Stops = MapModel.Stops,
            Lines = MapModel.Lines
        };
        MapModel = newModel;
    }

    private void RefreshView()
    {
        // Only animation visibility is kept in this ViewModel
        IsAnimationVisible = false;
    }
    #endregion

    #region Animations
    private async Task StartAnimation()
    {
        if (_tripManager.CurrentTrip is null) return;
        while (_tripManager.CurrentTrip.Status == TripState.Requested)
        {
            if (AnimatedImage == null) break;
            await Task.WhenAll(
                AnimatedImage.ScaleToAsync(1.5, 1500, Easing.SinOut),
                AnimatedImage.FadeToAsync(0.2, 1500, Easing.SinOut)
            );
            AnimatedImage.Scale = 1.0;
            AnimatedImage.Opacity = 1.0;
            await Task.Delay(100);
        }
    }
    #endregion
}
