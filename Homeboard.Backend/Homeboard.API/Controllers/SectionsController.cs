using FluentValidation;
using Homeboard.Boards.Dtos;
using Homeboard.Boards.Services;
using Microsoft.AspNetCore.Mvc;

namespace Homeboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SectionsController(
    ISectionCreator creator,
    ISectionUpdater updater,
    ISectionDeleter deleter,
    IValidator<CreateSectionDto> createValidator,
    IValidator<UpdateSectionDto> updateValidator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SectionDto>> Create([FromBody] CreateSectionDto dto, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(dto, ct);
        try
        {
            var section = await creator.CreateAsync(dto, ct);
            return Created($"/api/sections/{section.Id}", section);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSectionDto dto, CancellationToken ct)
    {
        await updateValidator.ValidateAndThrowAsync(dto, ct);
        try
        {
            var ok = await updater.UpdateAsync(id, dto, ct);
            return ok ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            return await deleter.DeleteAsync(id, ct) ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
