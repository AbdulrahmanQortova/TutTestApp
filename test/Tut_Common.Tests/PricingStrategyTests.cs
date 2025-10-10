using Tut.Common.Business;
using Tut.Common.Models;
using Xunit;

namespace Tut.Common.Tests;

public class PricingStrategyTests
{
    [Fact]
    public void BasicPricingStrategy_MinimalTrip_ReturnsBaseCost()
    {
        var strategy = new BasicPricingStrategy();
        var trip = new Trip
        {
            Stops = [],
            EstimatedDistance = 0,
            EstimatedTripDuration = 0
        };

        double price = strategy.Price(trip);

        // Base cost is 15
        Assert.Equal(15, price);
    }

    [Fact]
    public void BasicPricingStrategy_WithDistance_IncludesDistanceCost()
    {
        var strategy = new BasicPricingStrategy();
        var trip = new Trip
        {
            Stops = [],
            EstimatedDistance = 5000, // 5 km
            EstimatedTripDuration = 0
        };

        double price = strategy.Price(trip);

        // Base (15) + Distance (5 km * 10 = 50) = 65
        Assert.Equal(65, price);
    }

    [Fact]
    public void BasicPricingStrategy_WithDuration_IncludesTimeCost()
    {
        var strategy = new BasicPricingStrategy();
        var trip = new Trip
        {
            Stops = [],
            EstimatedDistance = 0,
            EstimatedTripDuration = 1800 // 30 minutes
        };

        double price = strategy.Price(trip);

        // Base (15) + Time (30 min * 1 = 30) = 45
        Assert.Equal(45, price);
    }

    [Fact]
    public void BasicPricingStrategy_CompleteTrip_CalculatesFullPrice()
    {
        var strategy = new BasicPricingStrategy();
        var trip = new Trip
        {
            Stops = [],
            EstimatedDistance = 10000, // 10 km
            EstimatedTripDuration = 1200 // 20 minutes
        };

        double price = strategy.Price(trip);

        // Base (15) + Distance (10 km * 10 = 100) + Time (20 min * 1 = 20) = 135
        Assert.Equal(135, price);
    }

    [Fact]
    public void BasicPricingStrategy_ShortTrip_ReturnsCorrectPrice()
    {
        var strategy = new BasicPricingStrategy();
        var trip = new Trip
        {
            Stops = [],
            EstimatedDistance = 1000, // 1 km
            EstimatedTripDuration = 300 // 5 minutes
        };

        double price = strategy.Price(trip);

        // Base (15) + Distance (1 km * 10 = 10) + Time (5 min * 1 = 5) = 30
        Assert.Equal(30, price);
    }

    [Fact]
    public void BasicPricingStrategy_LongTrip_ReturnsCorrectPrice()
    {
        var strategy = new BasicPricingStrategy();
        var trip = new Trip
        {
            Stops = [],
            EstimatedDistance = 50000, // 50 km
            EstimatedTripDuration = 3600 // 60 minutes
        };

        double price = strategy.Price(trip);

        // Base (15) + Distance (50 km * 10 = 500) + Time (60 min * 1 = 60) = 575
        Assert.Equal(575, price);
    }

    [Fact]
    public void BasicPricingStrategy_VeryShortDistance_CalculatesCorrectly()
    {
        var strategy = new BasicPricingStrategy();
        var trip = new Trip
        {
            Stops = [],
            EstimatedDistance = 100, // 0.1 km
            EstimatedTripDuration = 60 // 1 minute
        };

        double price = strategy.Price(trip);

        // Base (15) + Distance (0.1 km * 10 = 1) + Time (1 min * 1 = 1) = 17
        Assert.Equal(17, price);
    }

    [Fact]
    public void BasicPricingStrategy_DistanceOnly_NoTimeCharge()
    {
        var strategy = new BasicPricingStrategy();
        var trip = new Trip
        {
            Stops = [],
            EstimatedDistance = 8000, // 8 km
            EstimatedTripDuration = 0
        };

        double price = strategy.Price(trip);

        // Base (15) + Distance (8 km * 10 = 80) = 95
        Assert.Equal(95, price);
    }

    [Fact]
    public void BasicPricingStrategy_TimeOnly_NoDistanceCharge()
    {
        var strategy = new BasicPricingStrategy();
        var trip = new Trip
        {
            Stops = [],
            EstimatedDistance = 0,
            EstimatedTripDuration = 2400 // 40 minutes
        };

        double price = strategy.Price(trip);

        // Base (15) + Time (40 min * 1 = 40) = 55
        Assert.Equal(55, price);
    }

    [Fact]
    public void BasicPricingStrategy_FractionalValues_CalculatesCorrectly()
    {
        var strategy = new BasicPricingStrategy();
        var trip = new Trip
        {
            Stops = [],
            EstimatedDistance = 2500, // 2.5 km
            EstimatedTripDuration = 450 // 7.5 minutes
        };

        double price = strategy.Price(trip);

        // Base (15) + Distance (2.5 km * 10 = 25) + Time (7.5 min * 1 = 7.5) = 47.5
        Assert.Equal(47.5, price);
    }

    [Fact]
    public void IPricingStrategy_Interface_CanBeUsedPolymorphically()
    {
        IPricingStrategy strategy = new BasicPricingStrategy();
        var trip = new Trip
        {
            Stops = [],
            EstimatedDistance = 5000,
            EstimatedTripDuration = 600
        };

        double price = strategy.Price(trip);

        // Base (15) + Distance (5 km * 10 = 50) + Time (10 min * 1 = 10) = 75
        Assert.Equal(75, price);
    }
}

