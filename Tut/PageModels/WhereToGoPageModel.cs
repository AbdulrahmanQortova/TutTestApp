using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Tut.Common.Models;
using Tut.Pages;
using Tut.Utils;
using TutMauiCommon.Services;

namespace Tut.PageModels;

public class StopLocation // This class can remain as is
{
    public int Order { get; init; }
    public string Name { get; set; } = string.Empty;
    public string LocationStr { get; set; } = string.Empty;
    public Location? Location { get; init; }
}

public partial class WhereToGoPageModel(
    IShellService shellService,
    ILocationService locationService
) : ObservableObject, IQueryAttributable
{

    #region Properties

    [ObservableProperty] 
    private ObservableCollection<Stop> _stops = [];
    
    [ObservableProperty] 
    private bool _addStopPointVisibility;

    [ObservableProperty] 
    private Place? _selectedSavedPlace;

    [ObservableProperty]
    private List<Place> _savedPlaces = [];

    #endregion

    #region Commands

    [RelayCommand]
    private async Task OnStopLocationTappedAsync(Stop stop)
    {
        await Shell.Current.GoToAsync(nameof(SetLocationPage), new ShellNavigationQueryParameters
        {
            [StringConstants.Context] = stop.Id
        });
    }


    [RelayCommand]
    private async Task SubmitLocation()
    {

        List<Place> tripStops = [];
        tripStops.AddRange(Stops.Select(stop => stop.Place));

        if (tripStops.All(s => s.PlaceType != PlaceType.Unspecified))
        {
            Dictionary<string, object> args = new () {
                {
                    "Stops", tripStops
                }
            };
            await Shell.Current.GoToAsync(nameof(TripPage), args);
        }
        else
        {
            await shellService.DisplayAlertAsync("Error", "Please select both pickup and destination locations.", "OK");
        }
    }

    [RelayCommand]
    private void SavedPlaceSelected()
    {
        // Not Implemented Yet
    }

    [RelayCommand]
    private void RemoveStop(Stop stop)
    {
        Stops.Remove(stop);
    }

    [RelayCommand]
    private void AddStop()
    {
        Stops.Insert(Stops.Count-1, new Stop
        {
            Name = new Random().Next(20).ToString(),
            ShowDelete = true
        });
    }

    #endregion

    public async Task InitializeAsync()
    {
        if (Stops.Count == 0)
        {
            Stops.Add(new Stop
            {
                Name = "Select Pickup",
                Place = Place.NullPlace,
            });
            Stops.Add(new Stop
            {
                Name = "Select Destination",
                Place = Place.NullPlace,
                ShowAdd = true
            });
        }
        if (Stops[0].Place.Latitude < 0) 
        {
            Location? curLocation = await locationService.GetCurrentLocation(); // Use injected service
            if (curLocation != null)
            {
                Stops[0].Name = "Current Location";
                Stops[0].Place = new Place()
                {
                    PlaceType = PlaceType.Location,
                    Latitude = curLocation.Latitude,
                    Longitude = curLocation.Longitude,
                    Name = "Current Location",
                };
            }
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("context", out object? value) || value is not string contextString ||
            !query.TryGetValue("Place", out object? placeObject) || placeObject is not Place place) return;

        foreach (Stop stop in Stops)
        {
            if (contextString != stop.Id) continue;
            stop.Name = place.Name;
            stop.Place = place;
            break;
        }
    }

}

public partial class Stop : ObservableObject
{
    public string Id { get; } = Guid.NewGuid().ToString();
    
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private Place _place = Place.NullPlace;

    [ObservableProperty]
    private bool _showAdd;

    [ObservableProperty]
    private bool _showDelete;
}
