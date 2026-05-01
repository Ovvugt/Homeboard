using Homeboard.Widgets.Dtos;
using Homeboard.Widgets.Services;
using Microsoft.AspNetCore.Mvc;

namespace Homeboard.API.Controllers;

[ApiController]
[Route("api/widgets/[controller]")]
public sealed class WeatherController(IWeatherFetcher fetcher, ILogger<WeatherController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<WeatherDto>> Get(
        [FromQuery] double lat,
        [FromQuery] double lon,
        CancellationToken ct)
    {
        try
        {
            var dto = await fetcher.GetCurrentAsync(lat, lon, ct);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Weather fetch failed for {Lat},{Lon}", lat, lon);
            var detail = ex.InnerException is null
                ? $"{ex.GetType().Name}: {ex.Message}"
                : $"{ex.GetType().Name}: {ex.Message} -> {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
            return StatusCode(502, new { error = detail });
        }
    }
}
