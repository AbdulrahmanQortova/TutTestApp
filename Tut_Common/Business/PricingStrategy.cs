using Tut.Common.Models;
namespace Tut.Common.Business;


public interface IPricingStrategy
{
    double EstimatedPrice(Trip trip);
    double FinalPrice(Trip trip);
}
public class BasicPricingStrategy : IPricingStrategy
{
    private const double BaseConstantCost = 15;
    private const double PricePerKm = 10;
    private const double PricePerMinute = 1;
    public double EstimatedPrice(Trip trip)
    {
        return BaseConstantCost + trip.EstimatedDistance * PricePerKm / 1000 + trip.EstimatedTripDuration * PricePerMinute / 60;
    }

    public double FinalPrice(Trip trip)
    {
        return BaseConstantCost + trip.ActualDistance * PricePerKm / 1000 + trip.ActualTripDuration * PricePerMinute / 60;
    }
    
}
