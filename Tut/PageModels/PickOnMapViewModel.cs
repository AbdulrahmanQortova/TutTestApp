using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tut.Common.Business;
using Tut.Common.Dto.MapDtos;
using Tut.Common.Models;
using Tut.Utils;
using TutMauiCommon.Services;
using TutMauiCommon.ViewModels;

namespace Tut.PageModels;

public partial class PickOnMapViewModel(
        IGeoService geoService,
        IShellService shellService
    ) : ObservableObject, IQueryAttributable
{
    #region Properties
    [ObservableProperty]
    private bool _showCenter = true;
    [ObservableProperty]
    private QMapModel _mapModel = new QMapModel();
    
    private string? _locationContext;


    [ObservableProperty]
    private double _selectedLatitude;
    [ObservableProperty]
    private double _selectedLongitude;
    [ObservableProperty] 
    private string _placeName = string.Empty;

    #endregion

    #region Commands
    [RelayCommand]
    private async Task ConfirmButton()
    {
        try
        {
            SearchLocationResultDto resultDto = await geoService.SearchByCoords(SelectedLatitude, SelectedLongitude,
                ApplicationProperties.GoogleApiKey);
            Place? place = null;
            if (resultDto.Results is { Count: > 0 }) // Ensure Results is not null and has items
            {
                var result = resultDto.Results[0];
                place = new Place 
                {
                    PlaceType = PlaceType.Location,
                    Name = result.FormattedAddress ?? string.Empty,
                    Address = result.FormattedAddress ?? string.Empty,
                    Latitude = result.Geometry?.Location?.Lat ?? SelectedLatitude,
                    Longitude = result.Geometry?.Location?.Lng ?? SelectedLongitude,
                };
            }

            // Use NavigationService from ViewModelBase
            await Shell.Current.GoToAsync("..", new ShellNavigationQueryParameters
            {
                [StringConstants.Context] = _locationContext ?? string.Empty,
                [StringConstants.Place] = place ?? Place.NullPlace // Pass a null Place if null to avoid null ref issues on receiving end
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ConfirmButton: {ex.Message}");
            await shellService.DisplayAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }

    #endregion
    #region Methods

    public Task OnNavigatedTo()
    {
        return Task.CompletedTask;
    }

    public void Reset()
    {
        ShowCenter = true;
        SelectedLatitude = -1;
        SelectedLongitude = -1;
        PlaceName = string.Empty;
    }

    #endregion


    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        bool hasContext = query.TryGetValue(StringConstants.Context, out object? context);
        _locationContext = hasContext && context != null ? context as string : string.Empty; // Ensure context is not null
    }
}