namespace Tut.Common.Dto.MapDtos;

    public class SearchLocationResultDto
    {
        public string? ErrorMessage { get; set; }
        public string? Status { get; set; }
        public List<SearchLocationItemDto>? Results { get; set; }
    }

    public class SearchLocationItemDto
    {
        public string? FormattedAddress { get; set; }
        public GeometryDto? Geometry { get; set; }
        public string? Name { get; set; }
    }

    public class GeometryDto
    {
        public GeometryLoccationDto? Location { get; set; }
    }

    public class GeometryLoccationDto
    {
        public double? Lat { get; set; }
        public double? Lng { get; set; }
    }

