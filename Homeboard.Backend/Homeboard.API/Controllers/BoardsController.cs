using FluentValidation;
using Homeboard.Boards.Dtos;
using Homeboard.Boards.Services;
using Microsoft.AspNetCore.Mvc;

namespace Homeboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BoardsController(
    IBoardReader reader,
    IBoardCreator creator,
    IBoardUpdater updater,
    IBoardDeleter deleter,
    ILayoutSaver layoutSaver,
    IValidator<CreateBoardDto> createValidator,
    IValidator<UpdateBoardDto> updateValidator,
    IValidator<SaveLayoutDto> layoutValidator) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<BoardSummaryDto>> List(CancellationToken ct)
        => await reader.ListAsync(ct);

    [HttpGet("{slug}")]
    public async Task<ActionResult<BoardDetailDto>> Get(string slug, CancellationToken ct)
    {
        var board = await reader.GetBySlugAsync(slug, ct);
        return board is null ? NotFound() : Ok(board);
    }

    [HttpPost]
    public async Task<ActionResult<BoardSummaryDto>> Create([FromBody] CreateBoardDto dto, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(dto, ct);
        try
        {
            var board = await creator.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(Get), new { slug = board.Slug }, board);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBoardDto dto, CancellationToken ct)
    {
        await updateValidator.ValidateAndThrowAsync(dto, ct);
        try
        {
            var ok = await updater.UpdateAsync(id, dto, ct);
            return ok ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => await deleter.DeleteAsync(id, ct) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/layout")]
    public async Task<IActionResult> SaveLayout(Guid id, [FromBody] SaveLayoutDto dto, CancellationToken ct)
    {
        await layoutValidator.ValidateAndThrowAsync(dto, ct);
        await layoutSaver.SaveAsync(id, dto, ct);
        return NoContent();
    }
}
