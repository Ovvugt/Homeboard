using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Homeboard.Widgets.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Homeboard.Widgets.Services;

public interface IWeatherFetcher
{
    Task<WeatherDto> GetCurrentAsync(double lat, double lon, CancellationToken ct);
}

public sealed class WeatherFetcher(
    IHttpClientFactory http,
    IMemoryCache cache,
    IConfiguration config,
    ILogger<WeatherFetcher> logger) : IWeatherFetcher
{
    public async Task<WeatherDto> GetCurrentAsync(double lat, double lon, CancellationToken ct)
    {
        var inv = CultureInfo.InvariantCulture;
        var latStr = lat.ToString("0.####", inv);
        var lonStr = lon.ToString("0.####", inv);
        var key = $"weather:{latStr}:{lonStr}";
        if (cache.TryGetValue<WeatherDto>(key, out var cached) && cached is not null)
        {
            return cached;
        }

        var client = http.CreateClient("openmeteo");
        client.Timeout = TimeSpan.FromSeconds(8);
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={latStr}&longitude={lonStr}"
                + "&current=temperature_2m,apparent_temperature,weather_code,wind_speed_10m,relative_humidity_2m";

        var resp = await client.GetFromJsonAsync<OpenMeteoResponse>(url, ct);
        if (resp?.Current is null)
        {
            throw new InvalidOperationException("Open-Meteo returned no current data.");
        }

        var dto = new WeatherDto(
            lat, lon,
            resp.Current.Temperature2m,
            resp.Current.ApparentTemperature,
            resp.Current.WeatherCode,
            resp.Current.WindSpeed10m,
            resp.Current.RelativeHumidity2m,
            DateTime.UtcNow);

        var minutes = config.GetValue<int?>("Weather:CacheMinutes") ?? 10;
        cache.Set(key, dto, TimeSpan.FromMinutes(minutes));
        logger.LogDebug("Fetched weather for {Lat},{Lon}", lat, lon);
        return dto;
    }

    private sealed record OpenMeteoResponse([property: JsonPropertyName("current")] CurrentBlock? Current);

    private sealed record CurrentBlock(
        [property: JsonPropertyName("temperature_2m")] double Temperature2m,
        [property: JsonPropertyName("apparent_temperature")] double? ApparentTemperature,
        [property: JsonPropertyName("weather_code")] int? WeatherCode,
        [property: JsonPropertyName("wind_speed_10m")] double? WindSpeed10m,
        [property: JsonPropertyName("relative_humidity_2m")] double? RelativeHumidity2m);
}
