namespace Homeboard.Widgets.Dtos;

public sealed record WeatherDto(
    double Latitude,
    double Longitude,
    double TemperatureC,
    double? ApparentTemperatureC,
    int? WeatherCode,
    double? WindSpeedKmh,
    double? Humidity,
    DateTime FetchedUtc);
