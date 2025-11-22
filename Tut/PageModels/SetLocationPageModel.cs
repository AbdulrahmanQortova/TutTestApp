using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Tut.Common.Business;
using Tut.Common.Dto.MapDtos;
using Tut.Common.Models;
using Tut.Pages;
using Tut.Utils;
namespace Tut.PageModels;

public partial class SetLocationPageModel(
        IGeoService geoService
    ) : ObservableObject, IQueryAttributable
{
    private string? _locationContext;
    private Place? _place;

    #region Properties

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Place> _places = [];

    [ObservableProperty]
    private bool _isGpsOff;

    [ObservableProperty]
    private Place _selectedPlace = Place.NullPlace;

    [ObservableProperty]
    private bool _showClearButton;

    #endregion

    partial void OnSearchTermChanged(string value)
    {
        ShowClearButton = !string.IsNullOrEmpty(value);
    }

    #region Commands

    [RelayCommand]
    private void ClearSearch()
    {
        SearchTerm = string.Empty;
        ShowClearButton = false;
        Places = [];
    }

    [RelayCommand]
    private async Task CompleteSearch()
    {
        if (!string.IsNullOrEmpty(SearchTerm))
        {
            SearchLocationResultDto searchDto = await geoService.SearchLocationByLocationName(SearchTerm, ApplicationProperties.GoogleApiKey);
            if (searchDto is { Results: not null })
            {
                List<Place> resultList = [];
                resultList.AddRange(searchDto.Results!.Take(10).Select(item => new Place
                    {
                        PlaceType = PlaceType.Location,
                        Latitude = item.Geometry?.Location?.Lat ?? -1,
                        Longitude = item.Geometry?.Location?.Lng ?? -1,
                        Name = item.Name ?? string.Empty,
                        Address = item.FormattedAddress ?? string.Empty,
                    })
                );
                Places = resultList.ToObservableCollection();
            }
        }
    }


    [RelayCommand]
    private async Task SetLocationOnMap()
    {
        await Shell.Current.GoToAsync(nameof(PickOnMapPage), new ShellNavigationQueryParameters
        {
            [StringConstants.Context] = _locationContext ?? string.Empty,
        });
    }

    [RelayCommand]
    private async Task PlacesSelectionChanged()
    {
        await Shell.Current.GoToAsync("..", new ShellNavigationQueryParameters
        {
            [StringConstants.Context] = _locationContext ?? string.Empty,
            [StringConstants.Place] = SelectedPlace
        });
    }


    [RelayCommand]
    private void Reset()
    {
        // Waiting to see if this is needed
    }

    #endregion


    public async Task OnNavigatedTo()
    {
        if (_place != null)
        {
            await Shell.Current.GoToAsync("..", new ShellNavigationQueryParameters
            {
                [StringConstants.Context] = _locationContext ?? string.Empty,
                [StringConstants.Place] = _place
            });
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(StringConstants.Context, out object? context)) return;
        if (query.TryGetValue(StringConstants.Place, out object? place))
        {
            _place = place as Place;
        }

        _locationContext = context as string;
    }

}