using System.Globalization;
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
    Task AppendHistoryAsync(Guid tileId, DateTime checkedUtc, StatusValue status, int? responseTimeMs, CancellationToken ct);
    Task PruneHistoryOlderThanAsync(DateTime cutoffUtc, CancellationToken ct);
    Task<IReadOnlyList<TileStatusHistoryPoint>> ListHistoryByBoardAsync(Guid boardId, int maxPerTile, DateTime sinceUtc, CancellationToken ct);
}

public sealed class StatusRepository(ISqliteConnectionFactory factory) : IStatusRepository
{
    // Microsoft.Data.Sqlite returns DateTime values from TEXT columns as Kind=Local (with the
    // local offset baked in), which breaks worker scheduling because we compare against
    // DateTime.UtcNow. We read date columns as raw strings and parse to UTC explicitly.
    private const string SelectColumns = """
        tile_id          AS TileId,
        status           AS Status,
        last_checked_utc AS LastCheckedUtc,
        last_up_utc      AS LastUpUtc,
        last_down_utc    AS LastDownUtc,
        response_time_ms AS ResponseTimeMs,
        note             AS Note
        """;

    public async Task<IReadOnlyList<TileStatusSnapshot>> ListAllAsync(CancellationToken ct)
    {
        await using var conn = factory.Create();
        var rows = await conn.QueryAsync<SnapshotRow>(
            $"SELECT {SelectColumns} FROM tile_status_snapshots");
        return rows.Select(MapSnapshot).ToList();
    }

    public async Task<IReadOnlyList<TileStatusSnapshot>> ListByBoardAsync(Guid boardId, CancellationToken ct)
    {
        await using var conn = factory.Create();
        var rows = await conn.QueryAsync<SnapshotRow>(
            $"""
            SELECT {SelectColumns}
              FROM tile_status_snapshots s
              JOIN tiles t ON t.id = s.tile_id
             WHERE t.board_id = @boardId
            """,
            new { boardId = boardId.ToString() });
        return rows.Select(MapSnapshot).ToList();
    }

    public async Task<TileStatusSnapshot?> GetAsync(Guid tileId, CancellationToken ct)
    {
        await using var conn = factory.Create();
        var row = await conn.QuerySingleOrDefaultAsync<SnapshotRow>(
            $"SELECT {SelectColumns} FROM tile_status_snapshots WHERE tile_id = @tileId",
            new { tileId = tileId.ToString() });
        return row is null ? null : MapSnapshot(row);
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
                LastCheckedUtc = ToUtcString(s.LastCheckedUtc),
                LastUpUtc = s.LastUpUtc.HasValue ? ToUtcString(s.LastUpUtc.Value) : null,
                LastDownUtc = s.LastDownUtc.HasValue ? ToUtcString(s.LastDownUtc.Value) : null,
                s.ResponseTimeMs,
                s.Note
            });
    }

    public async Task AppendHistoryAsync(Guid tileId, DateTime checkedUtc, StatusValue status, int? responseTimeMs, CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.ExecuteAsync(
            """
            INSERT INTO tile_status_history (tile_id, checked_utc, status, response_time_ms)
            VALUES (@TileId, @CheckedUtc, @Status, @ResponseTimeMs)
            """,
            new
            {
                TileId = tileId.ToString(),
                CheckedUtc = ToUtcString(checkedUtc),
                Status = status.ToString(),
                ResponseTimeMs = responseTimeMs,
            });
    }

    public async Task PruneHistoryOlderThanAsync(DateTime cutoffUtc, CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.ExecuteAsync(
            "DELETE FROM tile_status_history WHERE checked_utc < @cutoff",
            new { cutoff = ToUtcString(cutoffUtc) });
    }

    public async Task<IReadOnlyList<TileStatusHistoryPoint>> ListHistoryByBoardAsync(Guid boardId, int maxPerTile, DateTime sinceUtc, CancellationToken ct)
    {
        await using var conn = factory.Create();
        var rows = await conn.QueryAsync<HistoryRow>(
            """
            SELECT TileId, CheckedUtc, Status, ResponseTimeMs
              FROM (
                SELECT h.tile_id          AS TileId,
                       h.checked_utc      AS CheckedUtc,
                       h.status           AS Status,
                       h.response_time_ms AS ResponseTimeMs,
                       ROW_NUMBER() OVER (PARTITION BY h.tile_id ORDER BY h.checked_utc DESC) AS rn
                  FROM tile_status_history h
                  JOIN tiles t ON t.id = h.tile_id
                 WHERE t.board_id = @boardId
                   AND h.checked_utc >= @since
              )
             WHERE rn <= @max
             ORDER BY TileId, CheckedUtc
            """,
            new
            {
                boardId = boardId.ToString(),
                since = ToUtcString(sinceUtc),
                max = maxPerTile,
            });

        return rows
            .Select(r => new TileStatusHistoryPoint(
                Guid.Parse(r.TileId!),
                ParseUtc(r.CheckedUtc!),
                Enum.Parse<StatusValue>(r.Status!),
                r.ResponseTimeMs))
            .ToList();
    }

    private static TileStatusSnapshot MapSnapshot(SnapshotRow r) => new()
    {
        TileId = Guid.Parse(r.TileId!),
        Status = Enum.Parse<StatusValue>(r.Status!),
        LastCheckedUtc = ParseUtc(r.LastCheckedUtc!),
        LastUpUtc = string.IsNullOrEmpty(r.LastUpUtc) ? null : ParseUtc(r.LastUpUtc),
        LastDownUtc = string.IsNullOrEmpty(r.LastDownUtc) ? null : ParseUtc(r.LastDownUtc),
        ResponseTimeMs = r.ResponseTimeMs,
        Note = r.Note,
    };

    private static string ToUtcString(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
        return utc.ToString("O", CultureInfo.InvariantCulture);
    }

    private static DateTime ParseUtc(string s) =>
        DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    private sealed class SnapshotRow
    {
        public string? TileId { get; set; }
        public string? Status { get; set; }
        public string? LastCheckedUtc { get; set; }
        public string? LastUpUtc { get; set; }
        public string? LastDownUtc { get; set; }
        public int? ResponseTimeMs { get; set; }
        public string? Note { get; set; }
    }

    private sealed class HistoryRow
    {
        public string? TileId { get; set; }
        public string? CheckedUtc { get; set; }
        public string? Status { get; set; }
        public int? ResponseTimeMs { get; set; }
    }
}
