using Homeboard.Boards.Repositories;
using Homeboard.Status.Entities;
using Homeboard.Status.Repositories;

namespace Homeboard.Status.Services;

public interface IStatusForcer
{
    Task<TileStatusSnapshot?> CheckNowAsync(Guid tileId, CancellationToken ct);
}

public sealed class StatusForcer(
    ITileRepository tiles,
    IStatusRepository repo,
    IStatusChecker checker) : IStatusForcer
{
    public async Task<TileStatusSnapshot?> CheckNowAsync(Guid tileId, CancellationToken ct)
    {
        var tile = await tiles.GetByIdAsync(tileId, ct);
        if (tile is null || tile.StatusType == Boards.Entities.TileStatusType.None) return null;
        var previous = await repo.GetAsync(tileId, ct);
        var snapshot = await checker.CheckAsync(tile, previous, ct);
        await repo.UpsertAsync(snapshot, ct);
        return snapshot;
    }
}
