using Homeboard.Status.Entities;
using Homeboard.Status.Repositories;

namespace Homeboard.Status.Services;

public interface IStatusReader
{
    Task<IReadOnlyList<TileStatusSnapshot>> GetByBoardAsync(Guid boardId, CancellationToken ct);
    Task<TileStatusSnapshot?> GetByTileAsync(Guid tileId, CancellationToken ct);
    Task<IReadOnlyList<TileStatusHistoryPoint>> GetHistoryByBoardAsync(Guid boardId, int maxPerTile, CancellationToken ct);
}

public sealed class StatusReader(IStatusRepository repo) : IStatusReader
{
    public Task<IReadOnlyList<TileStatusSnapshot>> GetByBoardAsync(Guid boardId, CancellationToken ct)
        => repo.ListByBoardAsync(boardId, ct);

    public Task<TileStatusSnapshot?> GetByTileAsync(Guid tileId, CancellationToken ct)
        => repo.GetAsync(tileId, ct);

    public Task<IReadOnlyList<TileStatusHistoryPoint>> GetHistoryByBoardAsync(Guid boardId, int maxPerTile, CancellationToken ct)
    {
        var capped = Math.Clamp(maxPerTile, 1, 500);
        var since = DateTime.UtcNow.AddHours(-24);
        return repo.ListHistoryByBoardAsync(boardId, capped, since, ct);
    }
}
