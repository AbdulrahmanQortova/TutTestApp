using Mapsui.UI.Maui;
using TutBackOffice.PageModels;


namespace TutBackOffice.Pages;

public partial class LiveTrackingPage
{
    private bool _firstRun = true;
    private CancellationTokenSource? _cts;

    private readonly LiveTrackingPageModel _pageModel = new ();

    private readonly TripManager.TripManagerClient _tripManagerClient;

    private readonly System.Timers.Timer _autoHideTimer;

    public LiveTrackingPage()
    {
        BindingContext = _pageModel;
        InitializeComponent();

        _autoHideTimer = new System.Timers.Timer(3000); // 3 seconds

        NavigatedTo += async (sender, args) =>
        {
            if (_firstRun)
            {
                await Initialize();
                _firstRun = false;
            }
            await Start();
        };

        NavigatedFrom += async (sender, args) =>
        {
            await Stop();
        };
    }

    private async Task Initialize()
    {
        await _viewModel.Initialize();
        await CreateMap();

        // Initialize auto-hide timer
        _autoHideTimer.Elapsed += (sender, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (hoverCard != null)
                    hoverCard.IsVisible = false;
                _autoHideTimer.Stop();
            });
        };
        _autoHideTimer.AutoReset = false;

        // Hover card pointer handling (platform-specific parts preserved)
        if (hoverCard != null)
        {
            hoverCard.IsVisible = false;
            hoverCard.HandlerChanged += (sender, e) =>
            {
                if (hoverCard.Handler != null)
                {
#if WINDOWS
                    var uiElement = hoverCard.Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
                    if (uiElement != null)
                    {
                        uiElement.PointerEntered += (s, e) => { /* Keep visible while hovering */ };
                        uiElement.PointerExited += (s, e) => { hoverCard.IsVisible = false; };
                    }
#else
                    mapView.MapClicked += (s, e) => { hoverCard.IsVisible = false; };
#endif
                }
            };
        }
    }

    private Task Start()
    {
        if (_cts != null) return Task.CompletedTask;

        mapView.PinClicked += MapView_PinClicked;

        mapView.Pins.Clear();
        mapView.Drawables.Clear();

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => DrawLoop(_cts.Token));

        return Task.CompletedTask;
    }

    private async Task Stop()
    {
        mapView.PinClicked -= MapView_PinClicked;

        // Detach Navigator handler
        try
        {
            if (mapView?.Map?.Navigator != null)
                mapView.Map.Navigator.ViewportChanged -= Navigator_ViewportChanged;
        }
        catch
        {
            // ignore
        }

        if (_cts != null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
            _cts = null;
        }
    }

    private void FilterDriversClicked(object? sender, EventArgs e)
    {
        DriverList.IsVisible = !DriverList.IsVisible;
    }

    private void RecenterClicked(object? sender, EventArgs e)
    {
        _adjustViewport = true;
    }

    private async void MapView_PinClicked(object? sender, PinClickedEventArgs e)
    {
        try
        {
            if (e.Pin?.Tag != null)
            {
                string tagInfo = e.Pin.Tag.ToString()!;
                await UpdateHoverCard(tagInfo);
                _autoHideTimer.Start();
            }

            e.Handled = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MapView_PinClicked: {ex.Message}");
        }
    }

    private async Task UpdateHoverCard(string tagInfo)
    {
        try
        {
            if (tagInfo.Contains("Driver:") || tagInfo.Contains("Trips:"))
            {
                // This is a driver pin
                var driverInfo = tagInfo.Split('\n');
                string driverName = driverInfo[0]; // First line is the driver name
                string plateNumber = driverInfo.Length > 1 ? driverInfo[1] : "";
                string tripsInfo = driverInfo.Length > 2 ? driverInfo[2] : "";
                
                // Update hover card UI
                hoverCardTitle.Text = driverName; // Vehicle model or can be customized
                hoverCardPlate.Text = plateNumber;
                hoverCardStatus.Text = "Active";
                hoverCardDriverName.Text = driverName;

                // Set pickup and destination to empty or N/A for driver cards
                //hoverCardPickup.Text = "Current Location";
                hoverCardDestination.Text = tripsInfo;
                
                // Set the icon for driver
                hoverCardIcon.Source = "car_available2.png";
            }
            else if (tagInfo.Contains("Pickup Point"))
            {
                // This is a pickup point
                var pointInfo = tagInfo.Split('\n');
                string tripId = "Unknown Trip";
                string state = "Unknown";
                string driverName = "Unknown Driver";
                
                foreach (var line in pointInfo)
                {
                    if (line.StartsWith("Trip ID:"))
                        tripId = line.Replace("Trip ID:", "").Trim();
                    else if (line.StartsWith("State:"))
                        state = line.Replace("State:", "").Trim();
                    else if (line.StartsWith("DriverName:"))
                        driverName = line.Replace("DriverName:", "").Trim();
                }
                
                // Update hover card UI
                hoverCardTitle.Text = "Pickup Point";
                hoverCardPlate.Text = "Trip ID: " + tripId;
                hoverCardStatus.Text = "State: " + state;
                hoverCardDriverName.Text = "DriverName:" + driverName;
                
                // Set pickup and destination to empty or N/A for pickup points
                //hoverCardPickup.Text = "Waiting for pickup";
                hoverCardDestination.Text = "N/A";
                
                // Set the icon for pickup
                hoverCardIcon.Source = "start2.png";
            }
            else if (tagInfo.Contains("Destination Point"))
            {
                // This is a destination point
                var pointInfo = tagInfo.Split('\n');
                string tripId = "Unknown Trip";
                string state = "Unknown";
                string customerName = "Unknown User";
                
                foreach (var line in pointInfo)
                {
                    if (line.StartsWith("Trip ID:"))
                        tripId = line.Replace("Trip ID:", "").Trim();
                    else if (line.StartsWith("State:"))
                        state = line.Replace("State:", "").Trim();
                    else if (line.StartsWith("Customer:"))
                        customerName = line.Replace("Customer:", "").Trim();
                }
                
                // Update hover card UI
                hoverCardTitle.Text = "Destination Point: " + customerName;
                hoverCardPlate.Text = "Trip ID: "  + tripId;
                hoverCardStatus.Text = "State: " + state;
                hoverCardDriverName.Text = "User Information";
                
                // Set user information and destination coordinates
                //hoverCardPickup.Text = "User Name: " + customerName;
                
                // Try to extract destination coordinates from tagInfo
                string destinationCoordinates = "Unknown Location";
                string phoneNumber = "N/A";
                
                // Try to find phone number and coordinates in the tag info
                foreach (var line in pointInfo)
                {
                    if (line.Contains("Latitude") && line.Contains("Longitude"))
                    {
                        destinationCoordinates = line.Trim();
                    }
                    else if (line.Contains("Phone") || line.Contains("Tel"))
                    {
                        phoneNumber = line.Trim();
                    }
                }
                
                // If coordinates not found in tagInfo, try to get them from the trip list
                if (destinationCoordinates == "Unknown Location" && !string.IsNullOrEmpty(tripId))
                {
                    try
                    {
                        // Try to parse trip ID
                        if (int.TryParse(tripId, out int id))
                        {
                            // Get active trips and find the one with matching ID
                            var tripListTask = _tripManagerClient.GetActiveTripsAsync(new Empty());
                            await tripListTask.ResponseAsync; // Wait for the task to complete
                            var tripList = await tripListTask.ResponseAsync;
                            var trip = tripList.Trips.FirstOrDefault(t => t.Id == id);
                            
                            if (trip != null)
                            {
                                if (trip.Destination != null)
                                {
                                    destinationCoordinates = $"Lat: {trip.Destination.Latitude:F6}, Long: {trip.Destination.Longitude:F6}";
                                }
                                
                                // If we have UserData, display it
                                if (trip.UserData != null)
                                {
                                    //hoverCardPickup.Text = $"User Name: {trip.UserData.Name}";
                                    
                                    // If we have user ID, display it as phone (since actual phone isn't available)
                                    if (!string.IsNullOrEmpty(trip.UserData.Id))
                                    {
                                        phoneNumber = $"User ID: {trip.UserData.Id}";
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting trip details: {ex.Message}");
                    }
                }
                
                // Display phone number if available, otherwise display coordinates
                if (phoneNumber != "N/A")
                {
                    hoverCardDestination.Text = phoneNumber;
                }
                else
                {
                    hoverCardDestination.Text = "Destination: " + destinationCoordinates;
                }
                
                // Set the icon for destination
                hoverCardIcon.Source = "destination2.png";
            }
            else if (tagInfo.Contains("Trip ID:"))
            {
                // This is a trip route
                var tripInfo = tagInfo.Split('\n');
                string tripId = "Unknown Trip";
                string state = "Unknown";
                string driverName = "Unknown Driver";
                string customerName = "Unknown Customer";
                
                foreach (var line in tripInfo)
                {
                    if (line.StartsWith("Trip ID:"))
                        tripId = line.Replace("Trip ID:", "").Trim();
                    else if (line.StartsWith("State:"))
                        state = line.Replace("State:", "").Trim();
                    else if (line.StartsWith("Driver:"))
                        driverName = line.Replace("Driver:", "").Trim();
                    else if (line.StartsWith("Customer:"))
                        customerName = line.Replace("Customer:", "").Trim();
                }
                
                // Update hover card UI
                hoverCardTitle.Text = "Trip Route";
                hoverCardPlate.Text = tripId;
                hoverCardStatus.Text = state;
                hoverCardDriverName.Text = driverName;
                //hoverCardPickup.Text = "Pickup Location";
                hoverCardDestination.Text = "Destination";
                
                // Set the icon for trip
                hoverCardIcon.Source = "car_ontrip2.png";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating hover card: {ex.Message}");
            if (hoverCard != null)
            {
                hoverCard.IsVisible = false;
            }
        }
        
        // Position hover card
        PositionHoverCard();
    }
    
    private void PositionHoverCard()
    {
        try
        {
            if (hoverCard == null) return;
            
            hoverCard.HorizontalOptions = LayoutOptions.End;
            hoverCard.VerticalOptions = LayoutOptions.Start;
            hoverCard.Margin = new Thickness(0, 10, 10, 0);
            
            hoverCard.IsVisible = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error positioning hover card: {ex.Message}");
            
            if (hoverCard != null)
            {
                hoverCard.IsVisible = true;
            }
        }
    }
    
    // --- Viewport adjustment logic ---
    private bool _adjustViewport = true;
    // timestamp of last programmatic viewport change
    private DateTime _lastProgrammaticViewportChange = DateTime.MinValue;
    private readonly TimeSpan _programmaticViewportIgnoreWindow = TimeSpan.FromMilliseconds(800);

    private async Task CreateMap()
    {
        mapView.Map = await _mapService.CreateMap();

        // Attach Mapsui Navigator ViewportChanged to detect manual pan/zoom
        try
        {
            if (mapView?.Map?.Navigator != null)
            {
                mapView.Map.Navigator.ViewportChanged -= Navigator_ViewportChanged;
                mapView.Map.Navigator.ViewportChanged += Navigator_ViewportChanged;
            }
        }
        catch
        {
            // Ignore if API differs on platform
        }

        // Ignore any immediate ViewportChanged events caused by map initialization
        _lastProgrammaticViewportChange = DateTime.UtcNow;
    }

    // Called when viewport changes; distinguish programmatic vs user changes using timestamp
    private void Navigator_ViewportChanged(object? sender, EventArgs e)
    {
        try
        {
            if (DateTime.UtcNow - _lastProgrammaticViewportChange <= _programmaticViewportIgnoreWindow)
                return; // likely programmatic

            // treat as user-initiated
            _adjustViewport = false;
        }
        catch
        {
            // ignore
        }
    }

    private async Task DrawLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TripList tripList = await _tripManagerClient.GetActiveTripsAsync(new Empty());
            DriverList driverList = await _driverManagerClient.GetAllDriversAsync(new Empty());
            LocationList locationList = await _driverLocationClient.GetDriverLocationsAsync(new Empty());

            mapView.Drawables.Clear();
            mapView.Pins.Clear();

            DriverLocationIteration(driverList, locationList);
            TripLoopIteration(tripList);
            await Task.Delay(1000, cancellationToken);
        }
    }

    private void DriverLocationIteration(DriverList driverList, LocationList locationList)
    {
        try
        {
            var driverLocations = new List<Microsoft.Maui.Devices.Sensors.Location>();
            foreach (Location location in locationList.Locations)
            {
                DriverType? driverType = driverList.Drivers.SingleOrDefault(d => d.Id == int.Parse(location.DriverId));
                if (driverType == null) continue;
                DriverFilterRow? row = _viewModel.Drivers.FirstOrDefault(d => d.Id == driverType.Id);
                if (row == null || !row.Selected) continue;

                var pin = _mapService.DrawAPinWithIcon(mapView, new Microsoft.Maui.Devices.Sensors.Location
                {
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                },
                // choose image based on driver state
                (driverType.DriverState == DriverState.Requested) ? "car_requested.png"
                    : (driverType.DriverState == DriverState.OnTrip) ? "car_ontrip.png"
                    : (driverType.DriverState == DriverState.EnRoute) ? "car_enroute.png"
                    : "car_available.png",
                 driverType.Username,
                 "",
                 true,
                 (float)(location.Course + 45.0));

                pin.Tag = $"{driverType.Username}\n{driverType.CarPlateNo}\nTrips: {driverType.TotalTrips}";
                driverLocations.Add(new Microsoft.Maui.Devices.Sensors.Location(location.Latitude, location.Longitude));
            }

            if (driverLocations.Count > 0 && mapView?.Map?.Navigator != null)
            {
                if (_adjustViewport)
                {
                    double minLat = driverLocations.Min(l => l.Latitude);
                    double maxLat = driverLocations.Max(l => l.Latitude);
                    double minLon = driverLocations.Min(l => l.Longitude);
                    double maxLon = driverLocations.Max(l => l.Longitude);

                    double latMargin = (maxLat - minLat) * 0.1;
                    double lonMargin = (maxLon - minLon) * 0.1;
                    if (latMargin < 0.001) latMargin = 0.001;
                    if (lonMargin < 0.001) lonMargin = 0.001;

                    (double minX, double minY) = Mapsui.Projections.SphericalMercator.FromLonLat(minLon - lonMargin, minLat - latMargin);
                    (double maxX, double maxY) = Mapsui.Projections.SphericalMercator.FromLonLat(maxLon + lonMargin, maxLat + latMargin);

                    // record timestamp so the ViewportChanged handler can ignore the following event
                    _lastProgrammaticViewportChange = DateTime.UtcNow;
                    mapView.Map.Navigator.ZoomToBox(new Mapsui.MRect(minX, minY, maxX, maxY));
                }
            }
        }
        catch
        {
            // ignore and continue
        }
    }

    private void TripLoopIteration(TripList tripList)
    {
        try
        {
            foreach (Trip trip in tripList.Trips)
            {
                int driverId = int.Parse(trip.DriverData.Id);
                DriverFilterRow? row = _viewModel.Drivers.FirstOrDefault(d => d.Id == driverId);
                if (row == null || !row.Selected) continue;
/*
                var routeLine = _mapService.DrawARoute(mapView, trip.RoutePins,
                    new Microsoft.Maui.Devices.Sensors.Location(trip.Pickup.Latitude, trip.Pickup.Longitude),
                    new Microsoft.Maui.Devices.Sensors.Location(trip.Destination.Latitude, trip.Destination.Longitude),
                    $"Trip ID: {trip.Id}\nState: {trip.State}");
*/
                _mapService.DrawALine(mapView, new Microsoft.Maui.Devices.Sensors.Location(trip.Pickup.Latitude, trip.Pickup.Longitude)
                ,new Microsoft.Maui.Devices.Sensors.Location(trip.Destination.Latitude, trip.Destination.Longitude));

                /*
                if (routeLine != null)
                {
                    routeLine.Tag = $"Trip ID: {trip.Id}\nState: {trip.State}";
                    routeLine.IsClickable = true;
                    routeLine.StrokeWidth = 10;
                    routeLine.Clicked += RouteLine_Clicked;
                }
                */

                _mapService.DrawAPinWithIcon(mapView, new Microsoft.Maui.Devices.Sensors.Location(trip.Pickup.Latitude, trip.Pickup.Longitude), "start.png", $"{trip.Id}.Pickup", $"Pickup Point\nTrip ID: {trip.Id}");
                _mapService.DrawAPinWithIcon(mapView, new Microsoft.Maui.Devices.Sensors.Location(trip.Destination.Latitude, trip.Destination.Longitude), "destination.png", $"{trip.Id}.Destination", $"Destination Point\nTrip ID: {trip.Id}");
            }
        }
        catch
        {
            // ignore
        }
    }

    private async void RouteLine_Clicked(object? sender, DrawableClickedEventArgs e)
    {
        try
        {
            if (sender is Polyline polyline)
            {
                string tagInfo = polyline.Tag?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(tagInfo))
                {
                    await UpdateHoverCard(tagInfo);
                    _autoHideTimer.Start();
                }
            }
            else
            {
                if (hoverCard != null) hoverCard.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in RouteLine_Clicked: {ex.Message}");
        }
    }

}

