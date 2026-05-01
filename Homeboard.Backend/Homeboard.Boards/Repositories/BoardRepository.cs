using Dapper;
using Homeboard.Boards.Entities;
using Homeboard.Core.Data;

namespace Homeboard.Boards.Repositories;

public interface IBoardRepository
{
    Task<IReadOnlyList<Board>> ListAsync(CancellationToken ct);
    Task<Board?> GetBySlugAsync(string slug, CancellationToken ct);
    Task<Board?> GetByIdAsync(Guid id, CancellationToken ct);
    Task InsertAsync(Board board, CancellationToken ct);
    Task UpdateAsync(Board board, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public sealed class BoardRepository(ISqliteConnectionFactory factory) : IBoardRepository
{
    public async Task<IReadOnlyList<Board>> ListAsync(CancellationToken ct)
    {
        await using var conn = factory.Create();
        var rows = await conn.QueryAsync<Board>(
            "SELECT id, name, slug, sort_order AS SortOrder, grid_columns AS GridColumns, created_utc AS CreatedUtc, updated_utc AS UpdatedUtc FROM boards ORDER BY sort_order, name");
        return rows.ToList();
    }

    public async Task<Board?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        await using var conn = factory.Create();
        return await conn.QuerySingleOrDefaultAsync<Board>(
            "SELECT id, name, slug, sort_order AS SortOrder, grid_columns AS GridColumns, created_utc AS CreatedUtc, updated_utc AS UpdatedUtc FROM boards WHERE slug = @slug",
            new { slug });
    }

    public async Task<Board?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var conn = factory.Create();
        return await conn.QuerySingleOrDefaultAsync<Board>(
            "SELECT id, name, slug, sort_order AS SortOrder, grid_columns AS GridColumns, created_utc AS CreatedUtc, updated_utc AS UpdatedUtc FROM boards WHERE id = @id",
            new { id = id.ToString() });
    }

    public async Task InsertAsync(Board board, CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.ExecuteAsync(
            """
            INSERT INTO boards (id, name, slug, sort_order, grid_columns, created_utc, updated_utc)
            VALUES (@Id, @Name, @Slug, @SortOrder, @GridColumns, @CreatedUtc, @UpdatedUtc)
            """,
            new
            {
                Id = board.Id.ToString(),
                board.Name,
                board.Slug,
                board.SortOrder,
                board.GridColumns,
                CreatedUtc = board.CreatedUtc.ToString("O"),
                UpdatedUtc = board.UpdatedUtc.ToString("O")
            });
    }

    public async Task UpdateAsync(Board board, CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.ExecuteAsync(
            """
            UPDATE boards
               SET name = @Name, slug = @Slug, sort_order = @SortOrder,
                   grid_columns = @GridColumns, updated_utc = @UpdatedUtc
             WHERE id = @Id
            """,
            new
            {
                Id = board.Id.ToString(),
                board.Name,
                board.Slug,
                board.SortOrder,
                board.GridColumns,
                UpdatedUtc = board.UpdatedUtc.ToString("O")
            });
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var conn = factory.Create();
        var rows = await conn.ExecuteAsync(
            "DELETE FROM boards WHERE id = @id",
            new { id = id.ToString() });
        return rows > 0;
    }
}
