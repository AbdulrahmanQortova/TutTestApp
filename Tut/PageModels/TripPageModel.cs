using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using Tut.Common.Business;
using Tut.Common.Dto.MapDtos;
using Tut.Common.Managers;
using Tut.Common.Models;
using Tut.PageModels.Popups;
using Tut.Pages;
using Tut.Popups;
using Tut.Utils;
using TutMauiCommon.ViewModels;

namespace Tut.PageModels;

public partial class TripPageModel : ObservableObject, IQueryAttributable, IDisposable
{
    private readonly IUserTripManager _tripManager;
    private readonly INotificationService _notificationService;
    private readonly IPopupService _popupService;
    private readonly IGeoService _geoService;
    
    private CancellationTokenSource? _lifecycleCts;
    private bool _isDisposed;

    [ObservableProperty]
    private List<Place> _tripPlaces = [];

    [ObservableProperty]
    private Trip? _trip;

    [ObservableProperty]
    private RideDetailsViewModel _rideDetailsVm;

    [ObservableProperty]
    private QMapModel _mapModel = new();

    [ObservableProperty]
    private bool _isAnimationVisible;

    [ObservableProperty]
    private bool _isConnected = true;

    [ObservableProperty]
    private string _connectionStatusMessage = string.Empty;

    [ObservableProperty]
    private bool _canCancelTrip;

    public TripPageModel(
        IGeoService geoService,
        IPopupService popupService,
        INotificationService notificationService,
        IUserTripManager tripManager,
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
        _lifecycleCts?.Dispose();
        _lifecycleCts = new CancellationTokenSource();

        SubscribeToTripManagerEvents();
        
        try
        {
            await _tripManager.Connect(_lifecycleCts.Token);
            await InquireRideAsync();
            await InitializeMapWithRoute();
            ShowBeforeDriverAcceptance();
        }
        catch (OperationCanceledException)
        {
            // Navigation cancelled
        }
        catch (Exception)
        {
            // Handle connection error
            ConnectionStatusMessage = "Failed to connect to server";
            IsConnected = false;
        }
    }

    public async Task OnDisappearing()
    {
        UnsubscribeFromTripManagerEvents();
        
        if (_lifecycleCts != null)
        {
            await _lifecycleCts.CancelAsync();
        }
        
        try
        {
            await _tripManager.Disconnect();
        }
        catch
        {
            // Ignore disconnection errors
        }
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
            Status = TripState.Unspecified
        };
        
        RideDetailsVm.TripPlaces = TripPlaces.ToObservableCollection();
    }

    public void Reset()
    {
        MapModel.ClearAll();
    }

    private async Task CancelRide()
    {
        if (!CanCancelTrip || _tripManager.CurrentTrip == null) return;
        
        try
        {
            await _tripManager.SendCancelTripAsync(_lifecycleCts?.Token ?? CancellationToken.None);
            
            // Navigate back after cancellation
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            ConnectionStatusMessage = $"Failed to cancel trip: {ex.Message}";
        }
    }

    private void SubscribeToTripManagerEvents()
    {
        _tripManager.StatusChanged += OnTripManagerStatusChanged;
        _tripManager.ErrorReceived += OnTripManagerErrorReceived;
        _tripManager.ConnectionStateChanged += OnTripManagerConnectionStateChanged;
        _tripManager.InquireResultReceived += OnTripManagerInquireResultReceived;
        _tripManager.DriverLocationsReceived += OnTripManagerDriverLocationsReceived;
    }

    private void UnsubscribeFromTripManagerEvents()
    {
        _tripManager.StatusChanged -= OnTripManagerStatusChanged;
        _tripManager.ErrorReceived -= OnTripManagerErrorReceived;
        _tripManager.ConnectionStateChanged -= OnTripManagerConnectionStateChanged;
        _tripManager.InquireResultReceived -= OnTripManagerInquireResultReceived;
        _tripManager.DriverLocationsReceived -= OnTripManagerDriverLocationsReceived;
    }

    private void ShowBeforeDriverAcceptance()
    {
        SetBeforeRequestUiState();
        CanCancelTrip = false;
    }

    private void OnTripManagerStatusChanged(object? sender, StatusUpdateEventArgs e)
    {
        if (e.Trip is null) return;
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _ = HandleTripStateChangeSafe(e.Trip);
        });
    }

    private async Task HandleTripStateChangeSafe(Trip trip)
    {
        try
        {
            await HandleTripStateChange(trip);
        }
        catch (Exception ex)
        {
            ConnectionStatusMessage = $"Error handling trip state: {ex.Message}";
        }
    }

    private async Task HandleTripStateChange(Trip trip)
    {
        Trip = trip;
        
        switch (trip.Status)
        {
            case TripState.Requested:
                SetFindingDriverUiState();
                CanCancelTrip = true;
                break;
                
            case TripState.Acknowledged:
                // Pass through - driver received but hasn't accepted yet
                CanCancelTrip = true;
                break;
                
            case TripState.Accepted:
                SetWaitingDriverUiState();
                GetDriverData();
                CanCancelTrip = true;
                break;
                
            case TripState.DriverArrived:
                await ShowDriverArrivedNotification();
                CanCancelTrip = true;
                break;
                
            case TripState.Ongoing:
                await _popupService.ClosePopupAsync(Shell.Current);
                SetOnTripUiState();
                CanCancelTrip = false; // Cannot cancel once trip starts
                break;
                
            case TripState.AtStop:
                SetOnTripUiState();
                CanCancelTrip = false;
                break;
                
            case TripState.Arrived:
                await ShowTripArrivedPopup();
                CanCancelTrip = false;
                break;
                
            case TripState.Ended:
                await HandleTripEnded();
                CanCancelTrip = false;
                break;
                
            case TripState.Canceled:
                await HandleTripCanceled();
                CanCancelTrip = false;
                break;
        }
    }

    private async Task ShowDriverArrivedNotification()
    {
        await _notificationService.Show(new NotificationRequest
        {
            NotificationId = 1001,
            Title = "Tut",
            Description = "Your driver has arrived.",
            ReturningData = "DriverArrived",
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
                { nameof(ArrivedPopupModel.Icon), "drivericon.png" },
                { nameof(ArrivedPopupModel.Title), "Your driver has arrived." },
                { nameof(ArrivedPopupModel.Message), "Please meet the captain \nat the pickup point." },
            });
    }

    private async Task ShowTripArrivedPopup()
    {
        RideDetailsVm.RefreshViews();
        RideDetailsVm.ViewAfterDriverAcceptingVisibility = true;
        
        await _popupService.ShowPopupAsync<ArrivedPopup>(Shell.Current,
            options: PopupOptions.Empty,
            shellParameters: new Dictionary<string, object>
            {
                { nameof(ArrivedPopupModel.Icon), "money.png" },
                { nameof(ArrivedPopupModel.Title), "You've arrived." },
                { nameof(ArrivedPopupModel.Message), "Please proceed to pay the fare." },
                { nameof(ArrivedPopupModel.Money), $"{RideDetailsVm.Price:F0}" },
                { nameof(ArrivedPopupModel.Currency), "LE" },
            });
    }

    private async Task HandleTripEnded()
    {
        await _popupService.ClosePopupAsync(Shell.Current);
        RideDetailsVm.ViewAfterDriverAcceptingVisibility = false;
        
        await Shell.Current.GoToAsync(nameof(RatingPage), new ShellNavigationQueryParameters
        {
            { StringConstants.TripId, _tripManager.CurrentTrip?.Id ?? 0 }
        });
    }

    private async Task HandleTripCanceled()
    {
        await _popupService.ClosePopupAsync(Shell.Current);
        ConnectionStatusMessage = "Trip was canceled";
        
        // Navigate back
        await Shell.Current.GoToAsync("..");
    }

    private void OnTripManagerErrorReceived(object? sender, ErrorReceivedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ConnectionStatusMessage = e.ErrorText;
        });
    }

    private void OnTripManagerConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsConnected = e.NewState == ConnectionState.Connected;
            ConnectionStatusMessage = e.NewState switch
            {
                ConnectionState.Disconnected => "Disconnected from server",
                ConnectionState.Connecting => "Connecting...",
                ConnectionState.Connected => string.Empty,
                ConnectionState.Reconnecting => "Reconnecting...",
                _ => string.Empty
            };
        });
    }

    private void OnTripManagerInquireResultReceived(object? sender, InquireResultEventArgs e)
    {
        if (e.Trip == null) return;
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Trip = e.Trip;
            UpdateRideDetailsFromInquiry(e.Trip);
        });
    }

    private void OnTripManagerDriverLocationsReceived(object? sender, DriverLocationsReceivedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateDriverLocationsOnMap(e.Locations);
        });
    }

    private async Task RideDetailsVm_RideRequested()
    {
        await RequestRideAsync();
    }

    private async Task RideDetailsVm_RideCancelled()
    {
        await CancelRide();
    }

    private void RideDetailsVm_ChangeRidePressed(object? sender, EventArgs e)
    {
        // Navigate back to stop selection
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _ = Shell.Current.GoToAsync("..");
        });
    }
    
    private async Task InquireRideAsync()
    {
        if (Trip == null) return;
        
        try
        {
            await _tripManager.SendInquireTripAsync(Trip, _lifecycleCts?.Token ?? default);
        }
        catch (OperationCanceledException)
        {
            // Inquiry cancelled
        }
        catch (Exception ex)
        {
            ConnectionStatusMessage = $"Failed to inquire trip: {ex.Message}";
        }
    }
    
    private async Task RequestRideAsync()
    {
        if (Trip == null) return;
        
        IsAnimationVisible = true;
        
        try
        {
            await _tripManager.SendRequestTripAsync(Trip, _lifecycleCts?.Token ?? CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Request cancelled
        }
        catch (Exception ex)
        {
            ConnectionStatusMessage = $"Failed to request trip: {ex.Message}";
            IsAnimationVisible = false;
        }
    }

    private void GetDriverData()
    {
        if (_tripManager.CurrentTrip?.Driver == null) return;
        
        RideDetailsVm.Driver = _tripManager.CurrentTrip.Driver;
        RideDetailsVm.DriverName = RideDetailsVm.Driver.FullName;
        RideDetailsVm.CarNumber = "abc 123";
    }
    
    private void UpdateRideDetailsFromInquiry(Trip trip)
    {
        if (TripPlaces.Count == 0) return;
        
        RideDetailsVm.PickupAddress = TripPlaces[0].Address;
        RideDetailsVm.DestinationAddress = TripPlaces[^1].Address;
        RideDetailsVm.Price = (int)trip.EstimatedCost;
        
        double timeInMinutes = trip.EstimatedArrivalDuration / 60.0;
        RideDetailsVm.Eta = $"{(int)timeInMinutes} min away";
        RideDetailsVm.Time = DateTime.Now.AddMinutes(timeInMinutes).ToString("hh:mm tt");
        RideDetailsVm.RideType = "Private car";
    }
    
    private async Task InitializeMapWithRoute()
    {
        if (Trip == null || TripPlaces.Count < 2) return;
        
        try
        {
            // Get route from Google Maps API
            DirectionResponseDto? directionResponseDto =
                await _geoService.GetRouteDataAsync(
                    ApplicationProperties.GoogleApiKey,
                    TripPlaces[0].ToLocation(),
                    TripPlaces[^1].ToLocation(),
                    TripPlaces.Skip(1).Take(TripPlaces.Count - 2)
                        .Where(p => p is { Latitude: > 0, Longitude: > 0 })
                        .Select(p => p.ToLocation())
                        .ToList());

            if (directionResponseDto?.Routes is { Count: > 0 })
            {
                Trip.SetRoute(directionResponseDto.Routes[0]);
            }
            // Fallback to just showing endpoints
            UpdateMapWithRoute();
        }
        catch (Exception ex)
        {
            ConnectionStatusMessage = $"Failed to load route: {ex.Message}";
            UpdateMapWithRoute();
        }
    }
    
    private void UpdateMapWithRoute()
    {
        if (Trip == null || TripPlaces.Count < 2) return;
        
        MapModel.ClearAll();
        
        // Add endpoints
        MapModel.EndPoints.Add(new QMapModel.MapPoint
        {
            Location = new Location(TripPlaces[0].Latitude, TripPlaces[0].Longitude)
        });
        
        MapModel.EndPoints.Add(new QMapModel.MapPoint
        {
            Location = new Location(TripPlaces[^1].Latitude, TripPlaces[^1].Longitude)
        });
        
        // Add intermediate stops
        foreach (var stop in TripPlaces.Skip(1).Take(TripPlaces.Count - 2))
        {
            MapModel.Stops.Add(new QMapModel.MapPoint
            {
                Location = new Location(stop.Latitude, stop.Longitude)
            });
        }
        
        // Add route if available
        if (!string.IsNullOrEmpty(Trip.Route))
        {
            MapModel.Routes.Add(new QMapModel.MapRoute
            {
                Route = new Route(Trip.Route),
                Color = Colors.Blue,
                Thickness = 3
            });
        }
        
        MapModel.CalculateExtent();
        MapModel.OnChanged();
    }
    
    private void UpdateDriverLocationsOnMap(List<GLocation> locations)
    {
        if (locations.Count == 0) return;
        
        // Clear existing cars and add new ones
        MapModel.Cars.Clear();
        
        foreach (var location in locations)
        {
            MapModel.Cars.Add(new QMapModel.MapCar
            {
                Location = new Location(location.Latitude, location.Longitude),
                Color = Colors.Red
            });
        }
        
        MapModel.CalculateExtent();
        MapModel.OnChanged();
    }
    
    private void SetBeforeRequestUiState()
    {
        RefreshView();
        RideDetailsVm.RefreshViews();
        RideDetailsVm.ViewBeforeDriverAcceptingVisibility = true;
    }

    private void SetFindingDriverUiState()
    {
        RefreshView();
        RideDetailsVm.RefreshViews();
        RideDetailsVm.FindingDriverVisibility = true;
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

    private void RefreshView()
    {
        IsAnimationVisible = false;
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        
        if (disposing)
        {
            _lifecycleCts?.Cancel();
            _lifecycleCts?.Dispose();
            UnsubscribeFromTripManagerEvents();
        }
        
        _isDisposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
