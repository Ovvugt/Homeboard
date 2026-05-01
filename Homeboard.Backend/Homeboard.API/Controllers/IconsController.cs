using Homeboard.Icons.Services;
using Microsoft.AspNetCore.Mvc;

namespace Homeboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class IconsController(IIconResolver resolver) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string url, CancellationToken ct)
    {
        if (!resolver.TryGetHost(url, out _))
        {
            return BadRequest(new { error = "Invalid url." });
        }

        var entry = await resolver.GetForUrlAsync(url, ct);
        if (entry is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "public, max-age=86400";
        Response.Headers.ETag = $"\"{entry.Host}-{entry.FetchedUtc.Ticks}\"";
        return File(entry.Bytes, entry.ContentType);
    }
}
