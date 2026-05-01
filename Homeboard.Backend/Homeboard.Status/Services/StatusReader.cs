using Homeboard.Status.Entities;
using Homeboard.Status.Repositories;

namespace Homeboard.Status.Services;

public interface IStatusReader
{
    Task<IReadOnlyList<TileStatusSnapshot>> GetByBoardAsync(Guid boardId, CancellationToken ct);
    Task<TileStatusSnapshot?> GetByTileAsync(Guid tileId, CancellationToken ct);
}

public sealed class StatusReader(IStatusRepository repo) : IStatusReader
{
    public Task<IReadOnlyList<TileStatusSnapshot>> GetByBoardAsync(Guid boardId, CancellationToken ct)
        => repo.ListByBoardAsync(boardId, ct);

    public Task<TileStatusSnapshot?> GetByTileAsync(Guid tileId, CancellationToken ct)
        => repo.GetAsync(tileId, ct);
}
