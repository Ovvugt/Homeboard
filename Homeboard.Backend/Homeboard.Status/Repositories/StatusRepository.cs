using Dapper;
using Homeboard.Core.Data;
using Homeboard.Status.Entities;

namespace Homeboard.Status.Repositories;

public interface IStatusRepository
{
    Task<IReadOnlyList<TileStatusSnapshot>> ListAllAsync(CancellationToken ct);
    Task<IReadOnlyList<TileStatusSnapshot>> ListByBoardAsync(Guid boardId, CancellationToken ct);
    Task<TileStatusSnapshot?> GetAsync(Guid tileId, CancellationToken ct);
    Task UpsertAsync(TileStatusSnapshot snapshot, CancellationToken ct);
}

public sealed class StatusRepository(ISqliteConnectionFactory factory) : IStatusRepository
{
    private const string SelectColumns = """
        tile_id AS TileId,
        status AS Status,
        last_checked_utc AS LastCheckedUtc,
        last_up_utc AS LastUpUtc,
        last_down_utc AS LastDownUtc,
        response_time_ms AS ResponseTimeMs,
        note AS Note
        """;

    public async Task<IReadOnlyList<TileStatusSnapshot>> ListAllAsync(CancellationToken ct)
    {
        await using var conn = factory.Create();
        var rows = await conn.QueryAsync<TileStatusSnapshot>(
            $"SELECT {SelectColumns} FROM tile_status_snapshots");
        return rows.ToList();
    }

    public async Task<IReadOnlyList<TileStatusSnapshot>> ListByBoardAsync(Guid boardId, CancellationToken ct)
    {
        await using var conn = factory.Create();
        var rows = await conn.QueryAsync<TileStatusSnapshot>(
            $"""
            SELECT {SelectColumns}
              FROM tile_status_snapshots s
              JOIN tiles t ON t.id = s.tile_id
             WHERE t.board_id = @boardId
            """,
            new { boardId = boardId.ToString() });
        return rows.ToList();
    }

    public async Task<TileStatusSnapshot?> GetAsync(Guid tileId, CancellationToken ct)
    {
        await using var conn = factory.Create();
        return await conn.QuerySingleOrDefaultAsync<TileStatusSnapshot>(
            $"SELECT {SelectColumns} FROM tile_status_snapshots WHERE tile_id = @tileId",
            new { tileId = tileId.ToString() });
    }

    public async Task UpsertAsync(TileStatusSnapshot s, CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.ExecuteAsync(
            """
            INSERT INTO tile_status_snapshots
                (tile_id, status, last_checked_utc, last_up_utc, last_down_utc, response_time_ms, note)
            VALUES
                (@TileId, @Status, @LastCheckedUtc, @LastUpUtc, @LastDownUtc, @ResponseTimeMs, @Note)
            ON CONFLICT(tile_id) DO UPDATE SET
                status = excluded.status,
                last_checked_utc = excluded.last_checked_utc,
                last_up_utc = excluded.last_up_utc,
                last_down_utc = excluded.last_down_utc,
                response_time_ms = excluded.response_time_ms,
                note = excluded.note
            """,
            new
            {
                TileId = s.TileId.ToString(),
                Status = s.Status.ToString(),
                LastCheckedUtc = s.LastCheckedUtc.ToString("O"),
                LastUpUtc = s.LastUpUtc?.ToString("O"),
                LastDownUtc = s.LastDownUtc?.ToString("O"),
                s.ResponseTimeMs,
                s.Note
            });
    }
}
