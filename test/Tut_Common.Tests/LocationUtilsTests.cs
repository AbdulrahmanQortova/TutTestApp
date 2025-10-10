using Tut.Common.Models;
using Tut.Common.Utils;
using Xunit;

namespace Tut.Common.Tests;

public class LocationUtilsTests
{
    [Fact]
    public void DistanceInKm_SameLocation_ReturnsZero()
    {
        var loc = new GLocation { Latitude = 40.7128, Longitude = -74.0060 };
        
        double distance = LocationUtils.DistanceInKm(loc, loc);
        
        Assert.Equal(0, distance, precision: 5);
    }

    [Fact]
    public void DistanceInMeters_SameLocation_ReturnsZero()
    {
        var loc = new GLocation { Latitude = 51.5074, Longitude = -0.1278 };
        
        double distance = LocationUtils.DistanceInMeters(loc, loc);
        
        Assert.Equal(0, distance, precision: 3);
    }

    [Fact]
    public void DistanceInKm_KnownLocations_ReturnsApproximateDistance()
    {
        // New York to Los Angeles (approximately 3944 km)
        var newYork = new GLocation { Latitude = 40.7128, Longitude = -74.0060 };
        var losAngeles = new GLocation { Latitude = 34.0522, Longitude = -118.2437 };
        
        double distance = LocationUtils.DistanceInKm(newYork, losAngeles);
        
        // Using approximate calculation, expect within 10% of actual distance
        Assert.InRange(distance, 3500, 4400);
    }

    [Fact]
    public void DistanceInMeters_ShortDistance_ReturnsAccurateDistance()
    {
        // Two points approximately 1 km apart
        var loc1 = new GLocation { Latitude = 40.7128, Longitude = -74.0060 };
        var loc2 = new GLocation { Latitude = 40.7228, Longitude = -74.0060 };
        
        double distance = LocationUtils.DistanceInMeters(loc1, loc2);
        
        // Approximately 1.11 km = 1110 meters
        Assert.InRange(distance, 1000, 1200);
    }

    [Fact]
    public void TotalDistanceInMeters_EmptyList_ReturnsZero()
    {
        var locations = new List<GLocation>();
        
        double distance = LocationUtils.TotalDistanceInMeters(locations);
        
        Assert.Equal(0, distance);
    }

    [Fact]
    public void TotalDistanceInMeters_SingleLocation_ReturnsZero()
    {
        var locations = new List<GLocation>
        {
            new() { Latitude = 40.7128, Longitude = -74.0060 }
        };
        
        double distance = LocationUtils.TotalDistanceInMeters(locations);
        
        Assert.Equal(0, distance);
    }

    [Fact]
    public void TotalDistanceInMeters_TwoLocations_ReturnsSingleDistance()
    {
        var loc1 = new GLocation { Latitude = 40.7128, Longitude = -74.0060 };
        var loc2 = new GLocation { Latitude = 40.7228, Longitude = -74.0060 };
        var locations = new List<GLocation> { loc1, loc2 };
        
        double totalDistance = LocationUtils.TotalDistanceInMeters(locations);
        double singleDistance = LocationUtils.DistanceInMeters(loc1, loc2);
        
        Assert.Equal(singleDistance, totalDistance, precision: 3);
    }

    [Fact]
    public void TotalDistanceInMeters_MultipleLocations_ReturnsSumOfDistances()
    {
        var loc1 = new GLocation { Latitude = 40.7128, Longitude = -74.0060 };
        var loc2 = new GLocation { Latitude = 40.7228, Longitude = -74.0060 };
        var loc3 = new GLocation { Latitude = 40.7328, Longitude = -74.0160 };
        var locations = new List<GLocation> { loc1, loc2, loc3 };
        
        double totalDistance = LocationUtils.TotalDistanceInMeters(locations);
        double dist1 = LocationUtils.DistanceInMeters(loc1, loc2);
        double dist2 = LocationUtils.DistanceInMeters(loc2, loc3);
        
        Assert.Equal(dist1 + dist2, totalDistance, precision: 3);
    }

    [Fact]
    public void Towards_ZeroDistance_ReturnsSourceLocation()
    {
        var src = new GLocation { Latitude = 40.7128, Longitude = -74.0060 };
        var dst = new GLocation { Latitude = 40.7228, Longitude = -74.0160 };
        
        var result = LocationUtils.Towards(src, dst, 0);
        
        Assert.Equal(src.Latitude, result.Latitude);
        Assert.Equal(src.Longitude, result.Longitude);
    }

    [Fact]
    public void Towards_NegativeDistance_ReturnsSourceLocation()
    {
        var src = new GLocation { Latitude = 40.7128, Longitude = -74.0060 };
        var dst = new GLocation { Latitude = 40.7228, Longitude = -74.0160 };
        
        var result = LocationUtils.Towards(src, dst, -100);
        
        Assert.Equal(src.Latitude, result.Latitude);
        Assert.Equal(src.Longitude, result.Longitude);
    }

    [Fact]
    public void Towards_DistanceGreaterThanTotal_ReturnsDestinationLocation()
    {
        var src = new GLocation { Latitude = 40.7128, Longitude = -74.0060 };
        var dst = new GLocation { Latitude = 40.7228, Longitude = -74.0160 };
        double totalDistance = LocationUtils.DistanceInMeters(src, dst);
        
        var result = LocationUtils.Towards(src, dst, totalDistance + 100);
        
        Assert.Equal(dst.Latitude, result.Latitude);
        Assert.Equal(dst.Longitude, result.Longitude);
    }

    [Fact]
    public void Towards_HalfDistance_ReturnsMidpoint()
    {
        var src = new GLocation { Latitude = 40.0, Longitude = -74.0 };
        var dst = new GLocation { Latitude = 40.0, Longitude = -73.0 };
        double totalDistance = LocationUtils.DistanceInMeters(src, dst);
        
        var result = LocationUtils.Towards(src, dst, totalDistance / 2);
        
        // Should be approximately midpoint
        Assert.InRange(result.Latitude, 39.99, 40.01);
        Assert.InRange(result.Longitude, -73.51, -73.49);
    }

    [Fact]
    public void Towards_SameSourceAndDestination_ReturnsDestination()
    {
        var loc = new GLocation { Latitude = 40.7128, Longitude = -74.0060 };
        
        var result = LocationUtils.Towards(loc, loc, 100);
        
        Assert.Equal(loc.Latitude, result.Latitude);
        Assert.Equal(loc.Longitude, result.Longitude);
    }

    [Fact]
    public void SameLocation_IdenticalLocations_ReturnsTrue()
    {
        var loc1 = new GLocation { Latitude = 40.7128, Longitude = -74.0060 };
        var loc2 = new GLocation { Latitude = 40.7128, Longitude = -74.0060 };
        
        bool result = LocationUtils.SameLocation(loc1, loc2);
        
        Assert.True(result);
    }

    [Fact]
    public void SameLocation_WithinDefaultError_ReturnsTrue()
    {
        // Two locations 15 meters apart (default error is 20m)
        var loc1 = new GLocation { Latitude = 40.7128, Longitude = -74.0060 };
        var loc2 = new GLocation { Latitude = 40.71295, Longitude = -74.0060 };
        
        bool result = LocationUtils.SameLocation(loc1, loc2);
        
        Assert.True(result);
    }

    [Fact]
    public void SameLocation_BeyondDefaultError_ReturnsFalse()
    {
        // Two locations more than 20 meters apart
        var loc1 = new GLocation { Latitude = 40.7128, Longitude = -74.0060 };
        var loc2 = new GLocation { Latitude = 40.7135, Longitude = -74.0060 };
        
        bool result = LocationUtils.SameLocation(loc1, loc2);
        
        Assert.False(result);
    }

    [Fact]
    public void SameLocation_WithCustomError_ReturnsCorrectResult()
    {
        var loc1 = new GLocation { Latitude = 40.7128, Longitude = -74.0060 };
        var loc2 = new GLocation { Latitude = 40.7135, Longitude = -74.0060 };
        
        bool resultWithSmallError = LocationUtils.SameLocation(loc1, loc2, error: 50);
        bool resultWithLargeError = LocationUtils.SameLocation(loc1, loc2, error: 100);
        
        Assert.False(resultWithSmallError);
        Assert.True(resultWithLargeError);
    }

    [Fact]
    public void DistanceInKm_AcrossEquator_ReturnsCorrectDistance()
    {
        var north = new GLocation { Latitude = 10.0, Longitude = 0.0 };
        var south = new GLocation { Latitude = -10.0, Longitude = 0.0 };
        
        double distance = LocationUtils.DistanceInKm(north, south);
        
        // Approximately 2226 km (20 degrees at ~111.3 km per degree)
        Assert.InRange(distance, 2200, 2300);
    }

    [Fact]
    public void DistanceInMeters_VeryShortDistance_ReturnsAccurateValue()
    {
        // Two locations approximately 10 meters apart
        var loc1 = new GLocation { Latitude = 40.712800, Longitude = -74.006000 };
        var loc2 = new GLocation { Latitude = 40.712900, Longitude = -74.006000 };
        
        double distance = LocationUtils.DistanceInMeters(loc1, loc2);
        
        // Approximately 11 meters
        Assert.InRange(distance, 8, 15);
    }

    [Fact]
    public void TotalDistanceInMeters_RouteWithManyPoints_CalculatesCorrectly()
    {
        var locations = new List<GLocation>
        {
            new() { Latitude = 40.7128, Longitude = -74.0060 },
            new() { Latitude = 40.7148, Longitude = -74.0070 },
            new() { Latitude = 40.7168, Longitude = -74.0080 },
            new() { Latitude = 40.7188, Longitude = -74.0090 },
            new() { Latitude = 40.7208, Longitude = -74.0100 }
        };
        
        double totalDistance = LocationUtils.TotalDistanceInMeters(locations);
        
        // Should be positive and reasonable
        Assert.True(totalDistance > 0);
        Assert.InRange(totalDistance, 800, 1100);
    }
}
