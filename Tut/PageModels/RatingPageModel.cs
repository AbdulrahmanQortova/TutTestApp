
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TutMauiCommon.Services;
namespace Tut.PageModels;

public partial class RatingModel : ObservableObject
{
    [ObservableProperty]
    private string _imageName = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _note = string.Empty;

    [ObservableProperty]
    private int _rating;

    [ObservableProperty]
    private int _imageWidth = 38;

    [ObservableProperty]
    private int _imageHeight = 38;
}
public partial class ReasonModel : ObservableObject
{
    [ObservableProperty]
    private string _reason = string.Empty;

    [ObservableProperty]
    private string _selectedBackground = "#ffffff";

    [ObservableProperty]
    private int _height = 35;
}


public partial class RatingPageModel(
        IShellService shellService
    ) : ObservableObject, IQueryAttributable
{
    public ObservableCollection<RatingModel> Ratings { get; set; } =
    [
        new RatingModel
        {
            Name = "angry",
            Rating = 0,
            ImageName = "angry"
        },
        new RatingModel
        {
            Name = "boared",
            Rating = 1,
            ImageName = "boared"
        }, // Corrected spelling "bored"
        new RatingModel
        {
            Name = "sad",
            Rating = 2,
            ImageName = "sad"
        },
        new RatingModel
        {
            Name = "happy",
            Rating = 3,
            ImageName = "happy"
        },
        new RatingModel
        {
            Name = "love",
            Rating = 4,
            ImageName = "love"
        }
    ];
    public ObservableCollection<ReasonModel> Reasons { get; set; } = [
        new ReasonModel { Reason="Pickup" },
        new ReasonModel { Reason="Conversation"},
        new ReasonModel { Reason="Traffic"},
        new ReasonModel { Reason="Price"},
        new ReasonModel { Reason="Cleanliness"},
        new ReasonModel { Reason="Driving"}
    ];

    [ObservableProperty]
    private RatingModel? _selectedRating;

    [ObservableProperty]
    private ReasonModel? _selectedReason;

    [ObservableProperty]
    private int _tripId;
     
    [ObservableProperty]
    private bool _showNote;


    #region Commands
    [RelayCommand]
    private void SelectReason(ReasonModel reason)
    {
        if (SelectedReason != null)
        {
            SelectedReason.SelectedBackground = "#ffffff"; // Reset previous
            SelectedReason.Height = 35;
        }

        SelectedReason = reason;

        if (SelectedReason != null)
        {
            SelectedReason.SelectedBackground = "#E0B76B"; // Apply to new
            SelectedReason.Height = 40;
        }
    }

    [RelayCommand]
    private void SelectRating(RatingModel rating)
    {
        if (SelectedRating != null)
        {
            SelectedRating.ImageWidth = 38; // Reset previous
            SelectedRating.ImageHeight = 38;
        }

        SelectedRating = rating;

        if (SelectedRating != null)
        {
            SelectedRating.ImageWidth = 50; // Apply to new
            SelectedRating.ImageHeight = 50;

            if (SelectedRating.Rating < 4) // Use SelectedRating consistently
            {
                ShowNote = true;
                SelectedReason = null; // Reset reason when note is shown
                SelectedRating.Note = string.Empty; // Clear previous note
            }
            else
            {
                ShowNote = false;
            }
        }
    }
    [RelayCommand]
    private async Task Rating()
    {
        try
        {
            if (SelectedRating == null)
            {
                await Toast.Make("You should select a rating item to continue.", ToastDuration.Long).Show();
                return;  
            }

            if (SelectedRating.Rating < 4) // Requires reason and note
            {
                if (SelectedReason == null || string.IsNullOrWhiteSpace(SelectedReason.Reason))
                {
                    await Toast.Make("Please select the reason for your rating.", ToastDuration.Long).Show();
                    return;
                }
                if (string.IsNullOrWhiteSpace(SelectedRating.Note))
                {
                    await Toast.Make("Please add a note for your rating.", ToastDuration.Long).Show();
                }
            }
        }
        catch (Exception ex) // Catch specific exceptions if possible
        {
            await Toast.Make($"An error occurred: {ex.Message}", ToastDuration.Long).Show();
        }
    }
    [RelayCommand]
    private async Task CallSupport()
    {
        try
        {
            var num = "01111111111"; // Consider making this configurable
            if (PhoneDialer.Default.IsSupported)
            {
                PhoneDialer.Default.Open(num);
            }
        }
        catch (Exception ex) // Log or handle specific exceptions
        {
            await shellService.DisplayAlertAsync("Dialer Error", $"Could not open dialer: {ex.Message}", "OK");
        }
    }
    #endregion


    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            if (!query.TryGetValue("tripId", out object? value)) return;
            TripId = Convert.ToInt32(value);
            query.Clear(); // Clear query after processing
        }
        catch (Exception ex)
        {
            await shellService.DisplayAlertAsync("Query Error", ex.Message, "OK");
        }
    }
}