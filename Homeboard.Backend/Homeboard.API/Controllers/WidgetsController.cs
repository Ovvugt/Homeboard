using FluentValidation;
using Homeboard.Boards.Dtos;
using Homeboard.Boards.Services;
using Microsoft.AspNetCore.Mvc;

namespace Homeboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WidgetsController(
    IWidgetCreator creator,
    IWidgetUpdater updater,
    IWidgetDeleter deleter,
    IValidator<CreateWidgetDto> createValidator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<WidgetDto>> Create([FromBody] CreateWidgetDto dto, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(dto, ct);
        try
        {
            var widget = await creator.CreateAsync(dto, ct);
            return Created($"/api/widgets/{widget.Id}", widget);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWidgetDto dto, CancellationToken ct)
    {
        var ok = await updater.UpdateAsync(id, dto, ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => await deleter.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
