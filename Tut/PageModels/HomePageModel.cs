using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tut.Common.Models;
using Tut.Pages;
using TutMauiCommon.Services;


namespace Tut.PageModels;

public partial class HomePageModel : ObservableObject
{
    private readonly ILocationService _locationService;


    [ObservableProperty] private string _sourceLocationDisplay = string.Empty;

    [ObservableProperty] private string _imageFromCamera = string.Empty;

    [ObservableProperty] private ObservableCollection<Place> _quickPlaces = [];

    [ObservableProperty] private ObservableCollection<string> _offers = ["offer_banner_1.png", "offer_banner_2.png"];

    [ObservableProperty]
    private ObservableCollection<ServiceType> _services =
    [
        new("Private", "service_private.png", false),
        new("Schedule", "service_schedule.png", true),
        new("City To City", "service_intercity.png", false),
        new("Package", "service_package.png", false)
    ];

    #region Commands


    [RelayCommand]
    private void OnQuickPlaceSelected()
    {
        // Implement quick place selection logic here
    }

    [RelayCommand]
    private async Task OnWhereToTapped()
    {
        if (false)
        {
            /*
            await Shell.Current.GoToAsync(nameof(TripPage), // Use base.NavigationService
                new Dictionary<string, object>
                {
                    {
                        "Source", new Place
                        {
                            PlaceType = PlaceType.Stop,
                            Latitude = 30.00585,
                            Longitude = 31.22983,
                            Address = "10 Prince Waleed Bin-Thanyan Al-Saud",
                            Name = "كورتوفا"
                        }
                    },
                    {
                        "Destination", new Place
                        {
                            PlaceType = PlaceType.Stop,
                            Latitude = 30.0974,
                            Longitude = 31.3736,
                            Address = "123 Duckville",
                            Name = "Donald Duck House"
                        }
                    }
                });

            return;
            */
        }

        await Shell.Current.GoToAsync(nameof(WhereToGoPage), true);
    }

    #endregion

    public HomePageModel(
        ILocationService locationService
        )
    {
        _locationService = locationService;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await GetUserInfo();
        try
        {
            await _locationService.GetCurrentLocation();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }

        QuickPlaces.Clear();
        QuickPlaces.Add(new Place
        {
            PlaceType = PlaceType.Recent,
            Name = "City Stars",
            Address = "Masaken Al Mohandeseen, Nasr City",
            Latitude = 30.00585,
            Longitude = 31.22983,
        });
        QuickPlaces.Add(new Place
        {
            PlaceType = PlaceType.Saved,
            Name = "Home",
            Address = "Moshir Ahmed Ismail, Sharara Blk, Villa 19",
            Latitude = 30.01585,
            Longitude = 31.23983,
        });
    }

    private Task GetUserInfo()
    {
        return Task.CompletedTask;
    }

}

public record ServiceType(string Title, string Image, bool Promo);
