using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapsui.UI.Maui;
using Location = PegasusBackEnd.Protos.Location;

namespace TutDriver.PageModels
{
    [AddINotifyPropertyChangedInterface]
	public partial class RequestPageViewModel : BaseViewModel
	{
        #region Properties

        public bool RequestReceived { get; set; } = true;
        public bool ArrivedToClientVisibility { get; set; }
        public bool StartTripVisibility { get; set; }
        public bool EndTripVisibility { get; set; }
        public bool PaymentViewVisibility { get; set; }
        public bool ContactsVisibility { get; set; } = true;
        public bool StopVisibility { get; set; } 
        public bool IsDriverStartTrip { get; set; }
        public MapView? MapView { get; set; }
        public Mapsui.Map Map { get; set; } = new();
        
        public RequestDto _requestDto;
        #endregion

        public Location Pickup { get; set; } = new();
        public Location Destination { get; set; } = new();

        public List<Location> Stops { get; set; } = [];
        private int _stopIdx = 0;

        [ObservableProperty]
        private Trip? _trip;

        [ObservableProperty]
        private int _price;

        [ObservableProperty]
        private string _destinationString = "";

        [ObservableProperty]
        private bool _isChatStarted = false;

        #region Commands
        [RelayCommand]
        public void Received()
        {
            RequestReceived = false;
            ArrivedToClientVisibility = true;
            StartTripVisibility = false;
            IsDriverStartTrip = false;
            Trip = GrpcClient.Instance.trip;
            Price = (int)Trip.Price;
            //Draw 2 points
            Pickup = new Location()
            {
                Latitude = GrpcClient.Instance.pickup.Latitude,
                Longitude = GrpcClient.Instance.pickup.Longitude
            };
            Destination = new Location()
            {
                Latitude = GrpcClient.Instance.destination.Latitude,
                Longitude = GrpcClient.Instance.destination.Longitude
            };
            DestinationString = $"{Destination.Latitude}, {Destination.Longitude}";

            if (Trip.StopList != null && Trip.StopList.Stops != null && Trip.StopList.Stops.Count > 0)
            {
                Stops.AddRange(Trip.StopList.Stops.Select(ps => new Location
                {
                    Latitude = ps.StopLocation.Latitude,
                    Longitude = ps.StopLocation.Longitude
                }));
            }
            
            if (MapView != null)
            {
                MapUtils.DrawAPinWithIcon(MapView, Pickup, "start.png", "Start");
                MapUtils.DrawAPinWithIcon(MapView, Destination, "destination.png", "End");
                MapUtils.DrawARoute(MapView, Trip.RoutePins,
                    new Microsoft.Maui.Devices.Sensors.Location(Pickup.Latitude, Pickup.Longitude),
                    new Microsoft.Maui.Devices.Sensors.Location(Destination.Latitude, Destination.Longitude));
            }

        }

        [RelayCommand]
        public async Task Arrived()
        {
                await GrpcClient.Instance.NotifyArrivedAsync();
                ArrivedToClientVisibility = false;
                StartTripVisibility = true;
                EndTripVisibility = false;
                IsDriverStartTrip = false;
        }
        [RelayCommand]
        public async Task StartTrip()
        {
                await GrpcClient.Instance.StartTripAsync();
                StartTripVisibility = false;
                ArrivedToClientVisibility = false;
                IsDriverStartTrip = true;
                if (_stopIdx < Stops.Count)
                    StopVisibility = true;
                else
                    EndTripVisibility = true;
        }

        [RelayCommand]
        private void CompleteStop()
        {
            _stopIdx++;
            if (_stopIdx < Stops.Count)
                StopVisibility = true;
            else
            {
                StopVisibility = false;
                EndTripVisibility = true;
            }
        }
        
        [RelayCommand]
        public async Task EndTrip()
        {
            await GrpcClient.Instance.EndTripAsync();
            EndTripVisibility = false;
            PaymentViewVisibility = true;
            ContactsVisibility = false;
            IsDriverStartTrip = true;
        }
        [RelayCommand]
        public async Task PaymentDone()
        {
            await GrpcClient.Instance.NotifyPaidAsync();
            await PopupNavigation.Instance.PushAsync(new ToastPopup()
            {
                Message = "تم الدفع"
            });
            await Task.Delay(1000);
            if (PopupNavigation.Instance.PopupStack.Any())
            {
                await PopupNavigation.Instance.PopAllAsync();
            }
            ResetVisibility();
            ContactsVisibility = true;
            IsDriverStartTrip = true;

            await NavigationService.NavigateToAsync(nameof(HomePage));
        }
        [RelayCommand]
        private async Task OpenClientLocation() 
        {
            await MapUtils.NavigateToBuilding(new Microsoft.Maui.Devices.Sensors.Location(Pickup.Latitude, Pickup.Longitude), "Client Location");
        }
        [RelayCommand]
        private async Task OpenDestinationLocation()
        {
            await MapUtils.NavigateToBuilding(new Microsoft.Maui.Devices.Sensors.Location(Destination.Latitude, Destination.Longitude), "Destination Location");
        }

        [RelayCommand]
        private async Task OpenStopLocation()
        {
            await MapUtils.NavigateToBuilding(new Microsoft.Maui.Devices.Sensors.Location(Stops[_stopIdx].Latitude, Stops[_stopIdx].Longitude), "Stop Location");
        }
        
        
        [RelayCommand]
        private async Task OpenChat()
        {
            if (Trip == null) return;
            Dictionary<string, object> keyValuePairs = [];
            keyValuePairs.Add("Trip", Trip);
            await NavigationService.NavigateToAsync(nameof(ChatPage), keyValuePairs);
        }
        private void ResetVisibility()
        {
            try
            {
                RequestReceived = true;
                ArrivedToClientVisibility = false;
                StartTripVisibility = false;
                EndTripVisibility = false;
                PaymentViewVisibility = false;
                ContactsVisibility = true;   
                MapView?.Pins.Clear();
                MapView?.Drawables.Clear();
            }
            catch (Exception ex)
            {
                _errorMsg = ex.Message;
            }
        }
        #endregion 



        public RequestPageViewModel()
        {
            ResetVisibility();
            _requestDto = new RequestDto
            {
                //حديقة الطفل
                StartLocation = new Microsoft.Maui.Devices.Sensors.Location { Latitude= 30.062081944890274, Longitude= 31.34730801680715 },
                //سيتي ستارز
                EndLocation = new Microsoft.Maui.Devices.Sensors.Location { Latitude = 30.07287428149848, Longitude = 31.347922936405975 }
            };

            GrpcClient.Instance.ChatStarted += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        IsChatStarted = true;
                        await NotificationService.ShowNotification(
                            "Pegasus Drivers",
                            $"لديك رسالة من صاحب الطلب",""
                        );
                        await OpenChat();
                    }
                    catch (Exception ex)
                    {
                        // Prevent exceptions from propagating out of async void
                    }
                });
            };
        }
    }
}

