using FluentValidation;
using Homeboard.Boards.Dtos;
using Homeboard.Boards.Services;
using Microsoft.AspNetCore.Mvc;

namespace Homeboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TilesController(
    ITileCreator creator,
    ITileUpdater updater,
    ITileDeleter deleter,
    IValidator<CreateTileDto> createValidator,
    IValidator<UpdateTileDto> updateValidator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TileDto>> Create([FromBody] CreateTileDto dto, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(dto, ct);
        try
        {
            var tile = await creator.CreateAsync(dto, ct);
            return Created($"/api/tiles/{tile.Id}", tile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTileDto dto, CancellationToken ct)
    {
        await updateValidator.ValidateAndThrowAsync(dto, ct);
        var ok = await updater.UpdateAsync(id, dto, ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => await deleter.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
