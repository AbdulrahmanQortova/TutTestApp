using System.Collections.Generic;
using Tut.Common.Models;
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
    }
}

