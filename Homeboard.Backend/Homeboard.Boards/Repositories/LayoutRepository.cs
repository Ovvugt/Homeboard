using Dapper;
using Homeboard.Boards.Dtos;
using Homeboard.Core.Data;

namespace Homeboard.Boards.Repositories;

public interface ILayoutRepository
{
    Task SaveAsync(IReadOnlyList<LayoutItemDto> items, CancellationToken ct);
}

public sealed class LayoutRepository(ISqliteConnectionFactory factory) : ILayoutRepository
{
    public async Task SaveAsync(IReadOnlyList<LayoutItemDto> items, CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.OpenAsync(ct);
        await using var tx = (Microsoft.Data.Sqlite.SqliteTransaction)await conn.BeginTransactionAsync(ct);

        foreach (var item in items)
        {
            var sql = item.Kind == LayoutItemKind.Tile
                ? "UPDATE tiles SET grid_x = @GridX, grid_y = @GridY, grid_w = @GridW, grid_h = @GridH WHERE id = @Id"
                : "UPDATE widgets SET grid_x = @GridX, grid_y = @GridY, grid_w = @GridW, grid_h = @GridH WHERE id = @Id";
            await conn.ExecuteAsync(sql, new
            {
                Id = item.Id.ToString(),
                item.GridX,
                item.GridY,
                item.GridW,
                item.GridH
            }, tx);
        }

        await tx.CommitAsync(ct);
    }
}
