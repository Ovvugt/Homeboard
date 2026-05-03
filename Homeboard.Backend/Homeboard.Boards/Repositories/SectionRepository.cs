using Dapper;
using Homeboard.Boards.Entities;
using Homeboard.Core.Data;

namespace Homeboard.Boards.Repositories;

public interface ISectionRepository
{
    Task<IReadOnlyList<Section>> ListByBoardAsync(Guid boardId, CancellationToken ct);
    Task<Section?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Section?> GetRootForBoardAsync(Guid boardId, CancellationToken ct);
    Task InsertAsync(Section section, CancellationToken ct);
    Task UpdateAsync(Section section, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, Guid reparentTo, CancellationToken ct);
}

public sealed class SectionRepository(ISqliteConnectionFactory factory) : ISectionRepository
{
    private const string SelectColumns = """
        id AS Id,
        board_id AS BoardId,
        parent_id AS ParentId,
        name AS Name,
        sort_order AS SortOrder,
        collapsed AS Collapsed,
        created_utc AS CreatedUtc,
        updated_utc AS UpdatedUtc
        """;

    public async Task<IReadOnlyList<Section>> ListByBoardAsync(Guid boardId, CancellationToken ct)
    {
        await using var conn = factory.Create();
        var rows = await conn.QueryAsync<Section>(
            $"SELECT {SelectColumns} FROM sections WHERE board_id = @boardId ORDER BY sort_order, created_utc",
            new { boardId = boardId.ToString() });
        return rows.ToList();
    }

    public async Task<Section?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var conn = factory.Create();
        return await conn.QuerySingleOrDefaultAsync<Section>(
            $"SELECT {SelectColumns} FROM sections WHERE id = @id",
            new { id = id.ToString() });
    }

    public async Task<Section?> GetRootForBoardAsync(Guid boardId, CancellationToken ct)
    {
        await using var conn = factory.Create();
        return await conn.QuerySingleOrDefaultAsync<Section>(
            $"SELECT {SelectColumns} FROM sections WHERE board_id = @boardId AND parent_id IS NULL ORDER BY sort_order LIMIT 1",
            new { boardId = boardId.ToString() });
    }

    public async Task InsertAsync(Section section, CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.ExecuteAsync(
            """
            INSERT INTO sections (id, board_id, parent_id, name, sort_order, collapsed, created_utc, updated_utc)
            VALUES (@Id, @BoardId, @ParentId, @Name, @SortOrder, @Collapsed, @CreatedUtc, @UpdatedUtc)
            """,
            ToParams(section));
    }

    public async Task UpdateAsync(Section section, CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.ExecuteAsync(
            """
            UPDATE sections
               SET parent_id = @ParentId, name = @Name, sort_order = @SortOrder,
                   collapsed = @Collapsed, updated_utc = @UpdatedUtc
             WHERE id = @Id
            """,
            ToParams(section));
    }

    public async Task<bool> DeleteAsync(Guid id, Guid reparentTo, CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.OpenAsync(ct);
        await using var tx = (Microsoft.Data.Sqlite.SqliteTransaction)await conn.BeginTransactionAsync(ct);

        var idText = id.ToString();
        var reparentText = reparentTo.ToString();

        // Re-parent direct child sections to the target.
        await conn.ExecuteAsync(
            "UPDATE sections SET parent_id = @ReparentTo WHERE parent_id = @Id",
            new { Id = idText, ReparentTo = reparentText }, tx);

        // Re-home tiles and widgets to the target section.
        await conn.ExecuteAsync(
            "UPDATE tiles SET section_id = @ReparentTo WHERE section_id = @Id",
            new { Id = idText, ReparentTo = reparentText }, tx);
        await conn.ExecuteAsync(
            "UPDATE widgets SET section_id = @ReparentTo WHERE section_id = @Id",
            new { Id = idText, ReparentTo = reparentText }, tx);

        var rows = await conn.ExecuteAsync(
            "DELETE FROM sections WHERE id = @Id",
            new { Id = idText }, tx);

        await tx.CommitAsync(ct);
        return rows > 0;
    }

    private static object ToParams(Section s) => new
    {
        Id = s.Id.ToString(),
        BoardId = s.BoardId.ToString(),
        ParentId = s.ParentId?.ToString(),
        s.Name,
        s.SortOrder,
        Collapsed = s.Collapsed ? 1 : 0,
        CreatedUtc = s.CreatedUtc.ToString("O"),
        UpdatedUtc = s.UpdatedUtc.ToString("O")
    };
}
