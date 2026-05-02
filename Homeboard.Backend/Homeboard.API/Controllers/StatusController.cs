using Homeboard.Status.Entities;
using Homeboard.Status.Services;
using Microsoft.AspNetCore.Mvc;

namespace Homeboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StatusController(IStatusReader reader, IStatusForcer forcer) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<TileStatusSnapshot>> ListByBoard([FromQuery] Guid boardId, CancellationToken ct)
        => reader.GetByBoardAsync(boardId, ct);

    [HttpGet("history")]
    public Task<IReadOnlyList<TileStatusHistoryPoint>> ListHistoryByBoard(
        [FromQuery] Guid boardId,
        [FromQuery] int max = 240,
        CancellationToken ct = default)
        => reader.GetHistoryByBoardAsync(boardId, max, ct);

    [HttpPost("{tileId:guid}/check")]
    public async Task<ActionResult<TileStatusSnapshot>> CheckNow(Guid tileId, CancellationToken ct)
    {
        var snap = await forcer.CheckNowAsync(tileId, ct);
        return snap is null ? NotFound() : Ok(snap);
    }
}
