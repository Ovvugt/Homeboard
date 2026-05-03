using Dapper;
using Homeboard.Boards.Entities;
using Homeboard.Core.Data;

namespace Homeboard.Boards.Repositories;

public interface ITileRepository
{
    Task<IReadOnlyList<Tile>> ListByBoardAsync(Guid boardId, CancellationToken ct);
    Task<IReadOnlyList<Tile>> ListAllWithChecksAsync(CancellationToken ct);
    Task<Tile?> GetByIdAsync(Guid id, CancellationToken ct);
    Task InsertAsync(Tile tile, CancellationToken ct);
    Task UpdateAsync(Tile tile, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public sealed class TileRepository(ISqliteConnectionFactory factory) : ITileRepository
{
    private const string SelectColumns = """
        id AS Id,
        board_id AS BoardId,
        section_id AS SectionId,
        name AS Name,
        url AS Url,
        icon_url AS IconUrl,
        icon_kind AS IconKind,
        description AS Description,
        color AS Color,
        grid_x AS GridX,
        grid_y AS GridY,
        grid_w AS GridW,
        grid_h AS GridH,
        status_type AS StatusType,
        status_target AS StatusTarget,
        status_interval AS StatusInterval,
        status_timeout AS StatusTimeout,
        status_expected AS StatusExpected
        """;

    public async Task<IReadOnlyList<Tile>> ListByBoardAsync(Guid boardId, CancellationToken ct)
    {
        await using var conn = factory.Create();
        var rows = await conn.QueryAsync<Tile>(
            $"SELECT {SelectColumns} FROM tiles WHERE board_id = @boardId ORDER BY grid_y, grid_x",
            new { boardId = boardId.ToString() });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<Tile>> ListAllWithChecksAsync(CancellationToken ct)
    {
        await using var conn = factory.Create();
        var rows = await conn.QueryAsync<Tile>(
            $"SELECT {SelectColumns} FROM tiles WHERE status_type <> 'None'");
        return rows.ToList();
    }

    public async Task<Tile?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var conn = factory.Create();
        return await conn.QuerySingleOrDefaultAsync<Tile>(
            $"SELECT {SelectColumns} FROM tiles WHERE id = @id",
            new { id = id.ToString() });
    }

    public async Task InsertAsync(Tile tile, CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.ExecuteAsync(
            """
            INSERT INTO tiles
                (id, board_id, section_id, name, url, icon_url, icon_kind, description, color,
                 grid_x, grid_y, grid_w, grid_h,
                 status_type, status_target, status_interval, status_timeout, status_expected)
            VALUES
                (@Id, @BoardId, @SectionId, @Name, @Url, @IconUrl, @IconKind, @Description, @Color,
                 @GridX, @GridY, @GridW, @GridH,
                 @StatusType, @StatusTarget, @StatusInterval, @StatusTimeout, @StatusExpected)
            """,
            ToParams(tile));
    }

    public async Task UpdateAsync(Tile tile, CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.ExecuteAsync(
            """
            UPDATE tiles
               SET name = @Name, url = @Url, icon_url = @IconUrl, icon_kind = @IconKind,
                   description = @Description, color = @Color,
                   section_id = @SectionId,
                   grid_x = @GridX, grid_y = @GridY, grid_w = @GridW, grid_h = @GridH,
                   status_type = @StatusType, status_target = @StatusTarget,
                   status_interval = @StatusInterval, status_timeout = @StatusTimeout,
                   status_expected = @StatusExpected
             WHERE id = @Id
            """,
            ToParams(tile));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var conn = factory.Create();
        var rows = await conn.ExecuteAsync(
            "DELETE FROM tiles WHERE id = @id",
            new { id = id.ToString() });
        return rows > 0;
    }

    private static object ToParams(Tile t) => new
    {
        Id = t.Id.ToString(),
        BoardId = t.BoardId.ToString(),
        SectionId = t.SectionId?.ToString(),
        t.Name,
        t.Url,
        t.IconUrl,
        IconKind = t.IconKind.ToString(),
        t.Description,
        t.Color,
        t.GridX,
        t.GridY,
        t.GridW,
        t.GridH,
        StatusType = t.StatusType.ToString(),
        t.StatusTarget,
        t.StatusInterval,
        t.StatusTimeout,
        t.StatusExpected
    };
}
