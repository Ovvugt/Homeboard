using Homeboard.Widgets.Dtos;
using Homeboard.Widgets.Services;
using Microsoft.AspNetCore.Mvc;

namespace Homeboard.API.Controllers;

[ApiController]
[Route("api/widgets/[controller]")]
public sealed class MinecraftController(
    IMinecraftStatusFetcher fetcher,
    ILogger<MinecraftController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MinecraftStatusDto>> Get(
        [FromQuery] string host,
        [FromQuery] int? port,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return BadRequest(new { error = "host is required" });
        }

        try
        {
            var dto = await fetcher.GetStatusAsync(host, port ?? 25565, ct);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Minecraft status fetch failed for {Host}:{Port}", host, port);
            return StatusCode(502, new { error = $"{ex.GetType().Name}: {ex.Message}" });
        }
    }
}
