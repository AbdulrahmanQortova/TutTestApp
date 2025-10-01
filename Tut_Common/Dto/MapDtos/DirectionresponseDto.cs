namespace Tut.Common.Dto.MapDtos;

public class DirectionResponseDto
{
    public List<GeocodedWaypointDto>? GeocodedWaypoints { get; set; }
    public List<RouteDto>? Routes { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
}
public class GeocodedWaypointDto
{
    public string? GeocoderStatus { get; set; }
    public string? PlaceId { get; set; }
    public List<string>? Types { get; set; }
}
public class RouteDto
{
    public BoundsDto? Bounds { get; set; }
    public List<LegDto>? Legs { get; set; }
    public DirectionPolylineDto? OverviewPolyline { get; set; }
    public string? Summary { get; set; }
}
public class BoundsDto
{
    public LocationDto? Northeast { get; set; }
    public LocationDto? Southwest { get; set; }
}
public class LocationDto
{
    public double Lat { get; set; }
    public double Lng { get; set; }
}
public class LegDto
{
    public TextValueDto? Distance { get; set; }
    public TextValueDto? Duration { get; set; }
    public TextValueDto? DurationInTraffic { get; set; }
    public string? EndAddress { get; set; }
    public LocationDto? EndLocation { get; set; }
    public string? StartAddress { get; set; }
    public LocationDto? StartLocation { get; set; }
    public List<StepDto>? Steps { get; set; }
    public List<DirectionsViaWaypointDto>? ViaWaypoint { get; set; }
    public TimeZonedTextValueDto? DepartureTime { get; set; }
    public TimeZonedTextValueDto? ArrivalTime { get; set; }
}
public class TextValueDto
{
    public string? Text { get; set; }
    public double? Value { get; set; }
}
public class TimeZonedTextValueDto
{
    public string? TimeZone { get; set; }
    public string? Text { get; set; }
    public double? Value { get; set; }
}
public class DirectionsViaWaypointDto
{
    public LocationDto? Location { get; set; }
    public int? StepIndex { get; set; }
    public double? StepInterpolation { get; set; }
}
public class StepDto
{
    public TextValueDto? Distance { get; set; }
    public TextValueDto? Duration { get; set; }
    public LocationDto? EndLocation { get; set; }
    public string? HtmlInstructions { get; set; }
    public DirectionPolylineDto? Polyline { get; set; }
    public LocationDto? StartLocation { get; set; }
    public TravelMode? TravelMode { get; set; }
    public string? Maneuver { get; set; }
}
public class DirectionPolylineDto
{
    public string? Points { get; set; }
}
public enum TravelMode
{
    Driving,
    Walking,
    Bicycling,
    Transit
}
