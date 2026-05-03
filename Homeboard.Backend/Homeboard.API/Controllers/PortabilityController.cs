using Homeboard.Boards.Dtos;
using Homeboard.Boards.Services;
using Microsoft.AspNetCore.Mvc;

namespace Homeboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PortabilityController(
    IBoardExporter exporter,
    IBoardImporter importer) : ControllerBase
{
    [HttpGet("export")]
    public async Task<BoardExportDto> Export(CancellationToken ct)
        => await exporter.BuildAsync(ct);

    [HttpPost("import")]
    public async Task<ActionResult<ImportResultDto>> Import(
        [FromBody] BoardExportDto data,
        [FromQuery] bool overwrite = false,
        CancellationToken ct = default)
    {
        try
        {
            var result = await importer.ImportAsync(data, overwrite, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
