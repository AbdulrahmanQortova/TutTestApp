
using Tut.Common.Dto.MapDtos;
using Tut.Common.Models;
namespace Tut.Common.Business;

public interface IGeoService
{
    Task<SearchLocationResultDto> SearchLocationByLocationName(string locationName, string googleKey, double latitude = double.MinValue, double longitude = double.MinValue);
    Task<SearchLocationResultDto> SearchByCoords(double latitude, double longitude, string googleKey);
    Task<DirectionResponseDto?> GetRouteDataAsync(string apiKey, GLocation startLocation, GLocation endLocation);
    Task<DirectionResponseDto?> GetRouteDataAsync(string apiKey, GLocation startLocation, GLocation endLocation, List<GLocation> waypoints);
}