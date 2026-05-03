using Dapper;
using Homeboard.Boards.Entities;
using Homeboard.Core.Data;

namespace Homeboard.Boards.Repositories;

public interface IWidgetRepository
{
    Task<IReadOnlyList<Widget>> ListByBoardAsync(Guid boardId, CancellationToken ct);
    Task<Widget?> GetByIdAsync(Guid id, CancellationToken ct);
    Task InsertAsync(Widget widget, CancellationToken ct);
    Task UpdateAsync(Widget widget, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public sealed class WidgetRepository(ISqliteConnectionFactory factory) : IWidgetRepository
{
    private const string SelectColumns = """
        id AS Id,
        board_id AS BoardId,
        section_id AS SectionId,
        type AS Type,
        grid_x AS GridX,
        grid_y AS GridY,
        grid_w AS GridW,
        grid_h AS GridH,
        config_json AS ConfigJson
        """;

    public async Task<IReadOnlyList<Widget>> ListByBoardAsync(Guid boardId, CancellationToken ct)
    {
        await using var conn = factory.Create();
        var rows = await conn.QueryAsync<Widget>(
            $"SELECT {SelectColumns} FROM widgets WHERE board_id = @boardId ORDER BY grid_y, grid_x",
            new { boardId = boardId.ToString() });
        return rows.ToList();
    }

    public async Task<Widget?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var conn = factory.Create();
        return await conn.QuerySingleOrDefaultAsync<Widget>(
            $"SELECT {SelectColumns} FROM widgets WHERE id = @id",
            new { id = id.ToString() });
    }

    public async Task InsertAsync(Widget widget, CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.ExecuteAsync(
            """
            INSERT INTO widgets (id, board_id, section_id, type, grid_x, grid_y, grid_w, grid_h, config_json)
            VALUES (@Id, @BoardId, @SectionId, @Type, @GridX, @GridY, @GridW, @GridH, @ConfigJson)
            """,
            new
            {
                Id = widget.Id.ToString(),
                BoardId = widget.BoardId.ToString(),
                SectionId = widget.SectionId?.ToString(),
                Type = widget.Type.ToString(),
                widget.GridX,
                widget.GridY,
                widget.GridW,
                widget.GridH,
                widget.ConfigJson
            });
    }

    public async Task UpdateAsync(Widget widget, CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.ExecuteAsync(
            """
            UPDATE widgets
               SET section_id = @SectionId,
                   grid_x = @GridX, grid_y = @GridY, grid_w = @GridW, grid_h = @GridH,
                   config_json = @ConfigJson
             WHERE id = @Id
            """,
            new
            {
                Id = widget.Id.ToString(),
                SectionId = widget.SectionId?.ToString(),
                widget.GridX,
                widget.GridY,
                widget.GridW,
                widget.GridH,
                widget.ConfigJson
            });
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var conn = factory.Create();
        var rows = await conn.ExecuteAsync(
            "DELETE FROM widgets WHERE id = @id",
            new { id = id.ToString() });
        return rows > 0;
    }
}
