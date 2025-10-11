using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tut.Common.Dto.MapDtos;
using Tut.Common.Models;
namespace Tut.Common.Business;

public class GeoService : IGeoService
{
    private static readonly HttpClient HttpClientInstance = new();
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter() }
    };

    private const string GooglePlacesTextSearchBaseUrl = "https://maps.googleapis.com/maps/api/place/textsearch/json";
    private const string GoogleGeocodeBaseUrl = "https://maps.googleapis.com/maps/api/geocode/json";
    private const string DefaultLanguage = "ar";
    private const string DefaultRegion = "EG";

    public async Task<DirectionResponseDto?> GetRouteDataAsync(string apiKey, GLocation startLocation, GLocation endLocation)
    {
        // Call the overload with an empty waypoint list
        return await GetRouteDataAsync(apiKey, startLocation, endLocation, []);
    }

    public async Task<DirectionResponseDto?> GetRouteDataAsync(string apiKey, GLocation startLocation, GLocation endLocation, List<GLocation> waypoints)
    {
        var waypointsParam = waypoints.Count > 0
            ? "&waypoints=" + string.Join("|", waypoints.Select(wp => $"{wp.Latitude},{wp.Longitude}"))
            : string.Empty;
        var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={startLocation.Latitude},{startLocation.Longitude}&destination={endLocation.Latitude},{endLocation.Longitude}{waypointsParam}&key={apiKey}";
        try
        {
            var response = await HttpClientInstance.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DirectionResponseDto>(json, JsonSerializerOptions);
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"HttpRequestException in GetRouteDataAsync (waypoints): {ex.Message}");
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"JsonException in GetRouteDataAsync (waypoints): {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in GetRouteDataAsync (waypoints): {ex.Message}");
        }
        return null;
    }

    public async Task<SearchLocationResultDto> SearchLocationByLocationName(string locationName, string googleKey, double latitude = double.MinValue, double longitude = double.MinValue)
    {
        try
        {
            string url = $"{GooglePlacesTextSearchBaseUrl}?query={Uri.EscapeDataString(locationName)}&radius=50000&key={googleKey}&language={DefaultLanguage}&region={DefaultRegion}";
            if (latitude > double.MinValue && longitude > double.MinValue)
                url += $"&location={latitude}%2C{longitude}";
            var response = await HttpClientInstance.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return CreateErrorResult($"Unable to retrieve location data (API Status: {response.StatusCode}).");
            string json = await response.Content.ReadAsStringAsync();
            SearchLocationResultDto? placesResponse = JsonSerializer.Deserialize<SearchLocationResultDto>(json, JsonSerializerOptions);
            return placesResponse ?? CreateErrorResult("Failed to deserialize location search response.");
        }
        catch (HttpRequestException ex)
        {
            return CreateErrorResult($"Network error occurred: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return CreateErrorResult($"Error processing location data: {ex.Message}");
        }
        catch (Exception ex)
        {
            return CreateErrorResult($"An unexpected error occurred while searching for the location. {ex.Message}");
        }
    }

    public async Task<SearchLocationResultDto> SearchByCoords(double latitude, double longitude, string googleKey)
    {
        try
        {
            string url = $"{GoogleGeocodeBaseUrl}?latlng={latitude},{longitude}&key={googleKey}&language={DefaultLanguage}";
            var response = await HttpClientInstance.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return CreateErrorResult($"Unable to retrieve location data from coordinates (API Status: {response.StatusCode}).");
            string json = await response.Content.ReadAsStringAsync();
            SearchLocationResultDto? placesResponse = JsonSerializer.Deserialize<SearchLocationResultDto>(json, JsonSerializerOptions);
            return placesResponse ?? CreateErrorResult("Failed to deserialize coordinate search response.");
        }
        catch (HttpRequestException ex)
        {
            return CreateErrorResult($"Network error occurred: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return CreateErrorResult($"Error processing location data from coordinates: {ex.Message}");
        }
        catch (Exception ex)
        {
            return CreateErrorResult($"An unexpected error occurred while searching by coordinates: {ex.Message}");
        }
    }

    private SearchLocationResultDto CreateErrorResult(string message)
    {
        return new SearchLocationResultDto()
        {
            ErrorMessage = message,
            Results = []
        };
    }
}