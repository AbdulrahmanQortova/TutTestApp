using System.Collections.Generic;
using Tut.Common.Models;
using Tut.Common.Utils;
using Xunit;

namespace Tut.Common.Tests
{
    public class TripTests
    {
        [Fact]
        public void CanCreateTrip_WithStops()
        {
            var trip = new Trip
            {
                Stops = new List<Place>()
            };

            Assert.NotNull(trip);
            Assert.NotNull(trip.Stops);
            Assert.Equal(0, trip.EstimatedDistance);
        }

        [Fact]
        public void Trip_DefaultValues_AreSetCorrectly()
        {
            var trip = new Trip
            {
                Stops = []
            };

            Assert.Equal(TripState.Unspecified, trip.Status);
            Assert.Equal(0, trip.EstimatedDistance);
            Assert.Equal(0, trip.ActualDistance);
            Assert.Equal(0, trip.TotalWaitTime);
            Assert.Equal(0, trip.EstimatedCost);
            Assert.Equal(0, trip.ActualCost);
            Assert.Equal(DateTime.MinValue, trip.DriverArrivalTime);
            Assert.Equal(DateTime.MinValue, trip.StartTime);
            Assert.Equal(DateTime.MinValue, trip.EndTime);
            Assert.Equal(DateTime.MinValue, trip.CancelTime);
        }

        [Fact]
        public void Trip_IsActive_UnspecifiedState_ReturnsFalse()
        {
            var trip = new Trip
            {
                Stops = [],
                Status = TripState.Unspecified
            };

            Assert.False(trip.IsActive);
        }

        [Fact]
        public void Trip_IsActive_EndedState_ReturnsFalse()
        {
            var trip = new Trip
            {
                Stops = [],
                Status = TripState.Ended
            };

            Assert.False(trip.IsActive);
        }

        [Fact]
        public void Trip_IsActive_CanceledState_ReturnsFalse()
        {
            var trip = new Trip
            {
                Stops = [],
                Status = TripState.Canceled
            };

            Assert.False(trip.IsActive);
        }

        [Fact]
        public void Trip_IsActive_RequestedState_ReturnsTrue()
        {
            var trip = new Trip
            {
                Stops = [],
                Status = TripState.Requested
            };

            Assert.True(trip.IsActive);
        }

        [Fact]
        public void Trip_IsActive_AcceptedState_ReturnsTrue()
        {
            var trip = new Trip
            {
                Stops = [],
                Status = TripState.Accepted
            };

            Assert.True(trip.IsActive);
        }

        [Fact]
        public void Trip_IsActive_DriverEnRouteState_ReturnsTrue()
        {
            var trip = new Trip
            {
                Stops = [],
                Status = TripState.Acknowledged
            };

            Assert.True(trip.IsActive);
        }

        [Fact]
        public void Trip_IsActive_InProgressState_ReturnsTrue()
        {
            var trip = new Trip
            {
                Stops = [],
                Status = TripState.Ongoing
            };

            Assert.True(trip.IsActive);
        }

        [Fact]
        public void SetRoute_WithValidRouteDto_SetsDistanceAndDuration()
        {
            var trip = new Trip
            {
                Stops = [
                    new Place { Latitude = 40.7128, Longitude = -74.0060, PlaceType = PlaceType.Stop, Order = 0 },
                    new Place { Latitude = 40.7580, Longitude = -73.9855, PlaceType = PlaceType.Stop, Order = 1 }
                ]
            };
            var routeDto = MockDtos.CreateRouteDto(
                distanceInMeters: 8500,
                durationInSeconds: 1200,
                polylinePoints: "abcd"
            );

            trip.SetRoute(routeDto);

            Assert.Equal(8500, trip.EstimatedDistance);
            Assert.Equal(1200, trip.EstimatedTripDuration);
            Assert.Equal("abcd", trip.Route);
        }

        [Fact]
        public void SetRoute_WithNullRouteDto_CalculatesStraightLineDistance()
        {
            var trip = new Trip
            {
                Stops = [
                    new Place { Latitude = 40.0, Longitude = -74.0, PlaceType = PlaceType.Stop, Order = 0 },
                    new Place { Latitude = 40.1, Longitude = -74.0, PlaceType = PlaceType.Stop, Order = 1 }
                ]
            };

            trip.SetRoute(null);

            // Straight line distance multiplied by 1.5
            var straightLineDistance = LocationUtils.TotalDistanceInMeters(
                trip.Stops.Select(s => s.ToLocation())
            );
            var expectedDistance = (int)(straightLineDistance * 1.5);

            Assert.Equal(expectedDistance, trip.EstimatedDistance);
            Assert.True(trip.EstimatedTripDuration > 0);
            Assert.Equal(string.Empty, trip.Route);
        }

        [Fact]
        public void SetRoute_WithNullPolyline_SetsEmptyRoute()
        {
            var trip = new Trip
            {
                Stops = [
                    new Place { Latitude = 40.7128, Longitude = -74.0060, PlaceType = PlaceType.Stop, Order = 0 }
                ]
            };
            var routeDto = MockDtos.CreateRouteDto(polylinePoints: null);

            trip.SetRoute(routeDto);

            Assert.Equal(string.Empty, trip.Route);
        }

        [Fact]
        public void SetRoute_WithMultipleStops_CalculatesDistanceCorrectly()
        {
            var trip = new Trip
            {
                Stops = [
                    new Place { Latitude = 40.7128, Longitude = -74.0060, PlaceType = PlaceType.Stop, Order = 0 },
                    new Place { Latitude = 40.7228, Longitude = -74.0160, PlaceType = PlaceType.Stop, Order = 1 },
                    new Place { Latitude = 40.7328, Longitude = -74.0260, PlaceType = PlaceType.Stop, Order = 2 }
                ]
            };

            trip.SetRoute(null);

            // Should calculate based on total straight line distance * 1.5
            Assert.True(trip.EstimatedDistance > 0);
            Assert.True(trip.EstimatedTripDuration > 0);
        }

        [Fact]
        public void SetRoute_WithZeroDistance_SetsZeroValues()
        {
            var trip = new Trip
            {
                Stops = []
            };
            var routeDto = MockDtos.CreateRouteDto(distanceInMeters: 0, durationInSeconds: 0);

            trip.SetRoute(routeDto);

            Assert.Equal(0, trip.EstimatedDistance);
            Assert.Equal(0, trip.EstimatedTripDuration);
        }

        [Fact]
        public void TripList_CanBeCreated_WithTrips()
        {
            var trips = new List<Trip>
            {
                new() { Stops = [] },
                new() { Stops = [] }
            };

            var tripList = new TripList(trips);

            Assert.NotNull(tripList.Trips);
            Assert.Equal(2, tripList.Trips.Count);
        }

        [Fact]
        public void TripList_CanBeCreated_Empty()
        {
            var tripList = new TripList();

            Assert.Null(tripList.Trips);
        }

        [Fact]
        public void Trip_CreatedAt_IsSetToUtcNow()
        {
            var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
            var trip = new Trip { Stops = [] };
            var afterCreation = DateTime.UtcNow.AddSeconds(1);

            Assert.InRange(trip.CreatedAt, beforeCreation, afterCreation);
        }

        [Fact]
        public void Trip_WithUserAndDriver_CanBeSet()
        {
            var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };
            var driver = new Driver { Id = 2, FirstName = "Jane", LastName = "Smith" };
            var trip = new Trip
            {
                Stops = [],
                User = user,
                Driver = driver
            };

            Assert.Equal(user, trip.User);
            Assert.Equal(driver, trip.Driver);
        }

        [Fact]
        public void Trip_WithPlaces_CanBeSet()
        {
            var requestingPlace = new Place
            {
                Latitude = 40.7128,
                Longitude = -74.0060,
                PlaceType = PlaceType.Recent,
                Order = 0
            };
            var trip = new Trip
            {
                Stops = [],
                RequestingPlace = requestingPlace
            };

            Assert.Equal(requestingPlace, trip.RequestingPlace);
        }
    }
}
