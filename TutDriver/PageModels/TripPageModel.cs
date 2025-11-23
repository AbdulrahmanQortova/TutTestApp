﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Tut.Common.Managers;
using Tut.Common.Models;
using TutMauiCommon.ViewModels;

namespace TutDriver.PageModels;

public partial class TripPageModel(
    DriverTripManager driverTripManager
    ) : ObservableObject
{
    [ObservableProperty]
    private QMapModel _mapModel = new();
    [ObservableProperty]
    private string _clientName = string.Empty;
    [ObservableProperty]
    private string _destination = string.Empty;
    [ObservableProperty]
    private string _nextStopTitle = string.Empty;
    [ObservableProperty]
    private string _requestedPaymentAmount = string.Empty;
    [ObservableProperty]
    private string _progressTripActionTitle = string.Empty;


    [RelayCommand]
    private async Task OpenLocationAsync()
    {
        if (driverTripManager.CurrentTrip is null) return;
        string locationName;
        if (driverTripManager.CurrentTrip.NextStop == 0)
            locationName = "Pickup Location";
        else if (driverTripManager.CurrentTrip.NextStop < driverTripManager.CurrentTrip.Stops.Count - 1)
            locationName = "Stop";
        else
            locationName = "Drop Off Location";
        
        Location destination = new()
        {
            Latitude = driverTripManager.CurrentTrip.Stops[driverTripManager.CurrentTrip.NextStop].Latitude,
            Longitude = driverTripManager.CurrentTrip.Stops[driverTripManager.CurrentTrip.NextStop].Longitude,
        };
        var options = new MapLaunchOptions { Name = locationName };
        try
        {
            await Map.Default.OpenAsync(destination, options);
        }
        catch (Exception)
        {
            // No map application available to open or placemark can not be located
            await Shell.Current.DisplayAlertAsync("Error", "Unable to open Map Application", "Ok");
        }

    }
    [RelayCommand]
    private async Task OpenClientChatAsync()
    {
        await Shell.Current.DisplayAlertAsync("Sorry", "Client Chat is not implemented yet", "Ok");
    }

    [RelayCommand]
    private async Task ProgressTripActionAsync()
    {
        if(driverTripManager.CurrentTrip is null) return;
        
        switch (driverTripManager.CurrentTrip.Status)
        {
            case TripState.Accepted:
                await driverTripManager.SendArrivedAtPickupAsync();
                break;
            case TripState.DriverArrived:
                await driverTripManager.SendStartTripAsync();
                break;
            case TripState.Ongoing:
                if (driverTripManager.CurrentTrip.NextStop < driverTripManager.CurrentTrip.Stops.Count - 1)
                    await driverTripManager.SendArrivedAtStopAsync();
                else
                    await driverTripManager.SendArrivedAtDestinationAsync();
                break;
            case TripState.AtStop:
                await driverTripManager.SendContinueTripAsync();
                break;
            case TripState.Arrived:
                await driverTripManager.SendCashPaymentMadeAsync((int)driverTripManager.CurrentTrip.ActualCost);
                break;
            case TripState.Ended:
            case TripState.Unspecified:
            case TripState.Requested:
            case TripState.Acknowledged:
            case TripState.Canceled:
                break;
            default:
                throw new ArgumentOutOfRangeException("Unknown trip status", new Exception("Dummy Inner Exception"));
        }
    }

    private void OnTripManagerStatusChanged(object? sender, StatusUpdateEventArgs e)
    {
        Trip? trip = e.Trip;
        if (trip is null)
        {
            // Navigate to HomePage
            Shell.Current.GoToAsync("..");
            return;
        }

        ClientName = trip.User?.FullName ?? string.Empty;
        Destination = string.IsNullOrEmpty(trip.Stops[^1].Name) ?
            trip.Stops[^1].Address :
            trip.Stops[^1].Name;
        
        
        if (trip.NextStop == 0)
        {
            NextStopTitle = "Pickup Location";
        } else if (trip.NextStop < trip.Stops.Count - 1)
        {
            NextStopTitle = "Next Stop";
        } else
        {
            NextStopTitle = "Drop Off Location";
        }

        switch (trip.Status)
        {
            case TripState.Accepted:
                ProgressTripActionTitle = "Arrived at Pickup";
                break;
            case TripState.DriverArrived:
                ProgressTripActionTitle = "Start Trip";
                break;
            case TripState.Ongoing:
                ProgressTripActionTitle = trip.NextStop < trip.Stops.Count - 1 ?
                    "Arrived At Stop" 
                    : "Arrived At Drop Off";
                break;
            case TripState.AtStop:
                ProgressTripActionTitle = "Continue Trip";
                break;
            case TripState.Arrived:
                ProgressTripActionTitle = "Confirm Payment";
                RequestedPaymentAmount = trip.ActualCost.ToString("#.##");
                break;
            case TripState.Ended:
                ProgressTripActionTitle = "Trip Ended";
                break;
            case TripState.Unspecified:
            case TripState.Requested:
            case TripState.Acknowledged:
            case TripState.Canceled:
                break;
            default:
                throw new ArgumentOutOfRangeException("Unknown trip status", new Exception("Dummy Inner Exception"));
        }
        List<QMapModel.MapRoute> routes = [];
        List<QMapModel.MapPoint> endPoints = [];
        List<QMapModel.MapPoint> stops = [];
        List<QMapModel.MapCar> cars = [];
        List<QMapModel.MapLine> lines = [];

        if (trip.Stops.Count < 2)
            return;
        endPoints.Add(new QMapModel.MapPoint()
        {
            Location = new Location(trip.Stops[0].Latitude, trip.Stops[0].Longitude),
            Color = Colors.Green
        });
        endPoints.Add(new QMapModel.MapPoint()
        {
            Location = new Location(trip.Stops[^1].Latitude, trip.Stops[^1].Longitude),
            Color = Colors.GreenYellow
        });
        for (int i = 1; i < trip.Stops.Count -1; i++)
        {
            if (trip.Stops.Count < i) 
                break;
            stops.Add(new QMapModel.MapPoint()
            {
                Location = new Location(trip.Stops[i].Latitude, trip.Stops[i].Longitude),
                Color = Colors.Yellow
            });
        }
        if (!string.IsNullOrEmpty(trip.Route))
        {
            routes.Add(new QMapModel.MapRoute
            {
                Route = new Route(trip.Route)
            });
        }
        else
        {
            lines.AddRange(GetSimpleRouteForTrip(trip));
        }
        
        QMapModel mapModel = new()
        {
            Routes = new ObservableCollection<QMapModel.MapRoute>(routes),
            EndPoints = new ObservableCollection<QMapModel.MapPoint>(endPoints),
            Stops = new ObservableCollection<QMapModel.MapPoint>(stops),
            Cars = new ObservableCollection<QMapModel.MapCar>(cars),
            Lines = new ObservableCollection<QMapModel.MapLine>(lines)
        };
        mapModel.CalculateExtent();
        mapModel.OnChanged();
        
        MapModel = mapModel;

    }


    private void OnTripManagerErrorReceived(object? sender, ErrorReceivedEventArgs e)
    {
        Shell.Current.DisplayAlertAsync("Error", "TripManager Error: " + e.ErrorText, "Ok");
    }
    
    private static List<QMapModel.MapLine> GetSimpleRouteForTrip(Trip trip, Color? color = null, int thickness = 1)
    {
        List<QMapModel.MapLine> result = [];
        for (int i = 1; i < trip.Stops.Count; i++)
        {
            QMapModel.MapLine line = new QMapModel.MapLine
            {
                StartPoint = new Location(trip.Stops[i-1].Latitude, trip.Stops[i-1].Longitude),
                EndPoint = new Location(trip.Stops[i].Latitude, trip.Stops[i].Longitude),
                Thickness = thickness
            };
            if(color != null)
                line.Color = color;
            
            result.Add(line);
        } 
        return result;
    }
    
    
    
    private bool _initialized;
    public async Task StartAsync()
    {
        if(!_initialized) await InitializeAsync();
        
        driverTripManager.StatusChanged += OnTripManagerStatusChanged;
        driverTripManager.ErrorReceived += OnTripManagerErrorReceived;
        
        OnTripManagerStatusChanged(this, new StatusUpdateEventArgs
        {
            Trip = driverTripManager.CurrentTrip
        });
    }


    public Task StopAsync()
    {
        driverTripManager.StatusChanged -= OnTripManagerStatusChanged;
        driverTripManager.ErrorReceived -= OnTripManagerErrorReceived;
        return Task.CompletedTask;
    }

    private Task InitializeAsync()
    {
        _initialized = true;
        return Task.CompletedTask;
    }

    
    
    
    
}
