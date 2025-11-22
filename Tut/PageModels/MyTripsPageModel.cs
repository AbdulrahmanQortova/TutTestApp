
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Tut.Common.GServices;
using Tut.Common.Models;
using TutMauiCommon.Services;
namespace Tut.PageModels
{
    public partial class MyTripsPageModel(
        IGTripManagerService tripManagerService,
        IShellService shellService) : ObservableObject
    {

        #region Properities
        [ObservableProperty]
        private ObservableCollection<Trip> _trips = [];

        [ObservableProperty]
        private Trip? _selectedTrip;

        [ObservableProperty]
        private bool _upcomingTripsVisible;
        [ObservableProperty]
        private bool _completedTripsVisible = true;

        #endregion

        #region Commands
        [RelayCommand]
        private void SelectCompleted()
        {
            UpcomingTripsVisible = false;
            CompletedTripsVisible = true;
        }

        [RelayCommand]
        private void SelectUpcoming()
        {
            CompletedTripsVisible = false;
            UpcomingTripsVisible = true;
        }

        [RelayCommand]
        private Task TripSelected(Trip trip)
        {
                SelectedTrip = trip;
/*
                await Shell.Current.GoToAsync(nameof(ViewTripPage), new ShellNavigationQueryParameters
                {
                    { StringConstants.SelectedTrip, trip }
                });
*/
            return Task.CompletedTask;
        }

        #endregion

        public async Task NavigatedToAsync()
        {
            SelectedTrip = null;
            await GetMyTrips();
        }

        private async Task GetMyTrips()
        {
            int status = CompletedTripsVisible ? 1 : 2;

            int userId = Preferences.Get("UserId",0);

            if (userId == 0)
            {
                await shellService.DisplayAlertAsync("Login Required", "Please, login first!", "OK");
                return; // Return early if user is not logged in
            }
            switch (status)
            {
                case 1: //Completed or canceled or suspended
                {
                    TripList completedTripsResponse = await tripManagerService.GetTripsForUser(new GPartialListIdRequest
                    {
                        Id =  userId,
                        Skip = 0,
                        Take = 20
                    });
                        Trips = completedTripsResponse.Trips
                            .Where(t => t.Status is TripState.Ended or TripState.Canceled)
                            .ToObservableCollection();
                    break;
                    }
                case 2: //Upcoming (Placeholder, original code was empty)
                    {
                        Trips.Clear(); // Clear for now as no specific logic was present
                        break;
                    }
            }
        }
    }
}
