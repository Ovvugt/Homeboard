using Dapper;
using Homeboard.Boards.Dtos;
using Homeboard.Boards.Entities;
using Homeboard.Boards.Repositories;
using Homeboard.Core.Data;
using Microsoft.Data.Sqlite;

namespace Homeboard.Boards.Services;

public static class PortabilityConstants
{
    public const int ExportVersion = 1;
}

public interface IBoardExporter
{
    Task<BoardExportDto> BuildAsync(CancellationToken ct);
}

public sealed class BoardExporter(
    IBoardRepository boards,
    ISectionRepository sections,
    ITileRepository tiles,
    IWidgetRepository widgets,
    TimeProvider time) : IBoardExporter
{
    public async Task<BoardExportDto> BuildAsync(CancellationToken ct)
    {
        var allBoards = (await boards.ListAsync(ct))
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var exported = new List<ExportedBoardDto>(allBoards.Count);
        foreach (var board in allBoards)
        {
            var sectionList = await sections.ListByBoardAsync(board.Id, ct);
            var tileList = await tiles.ListByBoardAsync(board.Id, ct);
            var widgetList = await widgets.ListByBoardAsync(board.Id, ct);

            exported.Add(new ExportedBoardDto(
                board.Name,
                board.Slug,
                board.SortOrder,
                board.GridColumns,
                sectionList.Select(s => new ExportedSectionDto(
                    s.Id, s.ParentId, s.Name, s.SortOrder, s.Collapsed)).ToList(),
                tileList.Select(t => new ExportedTileDto(
                    t.SectionId, t.Name, t.Url, t.IconUrl, t.IconKind, t.Description, t.Color,
                    t.GridX, t.GridY, t.GridW, t.GridH,
                    t.StatusType, t.StatusTarget, t.StatusInterval, t.StatusTimeout, t.StatusExpected)).ToList(),
                widgetList.Select(w => new ExportedWidgetDto(
                    w.SectionId, w.Type, w.GridX, w.GridY, w.GridW, w.GridH, w.ConfigJson)).ToList()));
        }

        return new BoardExportDto(
            PortabilityConstants.ExportVersion,
            time.GetUtcNow().UtcDateTime,
            exported);
    }
}

public interface IBoardImporter
{
    Task<ImportResultDto> ImportAsync(BoardExportDto data, bool overwrite, CancellationToken ct);
}

public sealed class BoardImporter(
    ISqliteConnectionFactory factory,
    TimeProvider time) : IBoardImporter
{
    public async Task<ImportResultDto> ImportAsync(BoardExportDto data, bool overwrite, CancellationToken ct)
    {
        if (data.Version != PortabilityConstants.ExportVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported export version {data.Version}. This server expects version {PortabilityConstants.ExportVersion}.");
        }

        var created = new List<string>();
        var replaced = new List<string>();
        var skipped = new List<ImportIssueDto>();
        var failed = new List<ImportIssueDto>();

        foreach (var board in data.Boards)
        {
            try
            {
                var outcome = await ImportSingleAsync(board, overwrite, ct);
                switch (outcome)
                {
                    case ImportOutcome.Created: created.Add(board.Slug); break;
                    case ImportOutcome.Replaced: replaced.Add(board.Slug); break;
                    case ImportOutcome.SkippedConflict:
                        skipped.Add(new ImportIssueDto(board.Slug, "A board with this slug already exists."));
                        break;
                }
            }
            catch (Exception ex)
            {
                failed.Add(new ImportIssueDto(board.Slug, ex.Message));
            }
        }

        return new ImportResultDto(created, replaced, skipped, failed);
    }

    private enum ImportOutcome { Created, Replaced, SkippedConflict }

    private async Task<ImportOutcome> ImportSingleAsync(ExportedBoardDto board, bool overwrite, CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.OpenAsync(ct);

        // Sanitize the slug the same way BoardCreator does so it matches whatever the UI would produce.
        var slug = SlugNormalizer.Normalize(string.IsNullOrWhiteSpace(board.Slug) ? board.Name : board.Slug);

        var existingBoardId = await conn.QuerySingleOrDefaultAsync<string?>(
            "SELECT id FROM boards WHERE slug = @slug",
            new { slug });

        if (existingBoardId is not null && !overwrite)
        {
            return ImportOutcome.SkippedConflict;
        }

        await using var tx = (SqliteTransaction)await conn.BeginTransactionAsync(ct);
        var nowText = time.GetUtcNow().UtcDateTime.ToString("O");

        if (existingBoardId is not null)
        {
            // Don't trust schema-level cascade (FK enforcement may not be enabled at runtime).
            await conn.ExecuteAsync(
                "DELETE FROM tile_status_snapshots WHERE tile_id IN (SELECT id FROM tiles WHERE board_id = @id)",
                new { id = existingBoardId }, tx);
            await conn.ExecuteAsync("DELETE FROM tiles    WHERE board_id = @id", new { id = existingBoardId }, tx);
            await conn.ExecuteAsync("DELETE FROM widgets  WHERE board_id = @id", new { id = existingBoardId }, tx);
            await conn.ExecuteAsync("DELETE FROM sections WHERE board_id = @id", new { id = existingBoardId }, tx);
            await conn.ExecuteAsync("DELETE FROM boards   WHERE id       = @id", new { id = existingBoardId }, tx);
        }

        var newBoardId = Guid.NewGuid();
        await conn.ExecuteAsync(
            """
            INSERT INTO boards (id, name, slug, sort_order, grid_columns, created_utc, updated_utc)
            VALUES (@Id, @Name, @Slug, @SortOrder, @GridColumns, @CreatedUtc, @UpdatedUtc)
            """,
            new
            {
                Id = newBoardId.ToString(),
                Name = board.Name.Trim(),
                Slug = slug,
                board.SortOrder,
                board.GridColumns,
                CreatedUtc = nowText,
                UpdatedUtc = nowText,
            }, tx);

        // Sections: re-create in parent-before-child order, mapping export-local IDs to fresh server IDs.
        var sectionIdMap = new Dictionary<Guid, Guid>();
        var remaining = new List<ExportedSectionDto>(board.Sections);
        var progress = true;
        while (remaining.Count > 0 && progress)
        {
            progress = false;
            for (var i = remaining.Count - 1; i >= 0; i--)
            {
                var s = remaining[i];
                if (s.ParentLocalId.HasValue && !sectionIdMap.ContainsKey(s.ParentLocalId.Value)) continue;
                var newId = Guid.NewGuid();
                Guid? parentNewId = s.ParentLocalId.HasValue ? sectionIdMap[s.ParentLocalId.Value] : null;
                await InsertSectionAsync(conn, tx, newId, newBoardId, parentNewId, s, nowText);
                sectionIdMap[s.LocalId] = newId;
                remaining.RemoveAt(i);
                progress = true;
            }
        }
        // Any leftovers had unresolvable parents -- create them as roots so their items still land somewhere.
        foreach (var s in remaining)
        {
            var newId = Guid.NewGuid();
            await InsertSectionAsync(conn, tx, newId, newBoardId, null, s, nowText);
            sectionIdMap[s.LocalId] = newId;
        }

        Guid? MapSection(Guid? localId) =>
            localId.HasValue && sectionIdMap.TryGetValue(localId.Value, out var newId) ? newId : null;

        foreach (var t in board.Tiles)
        {
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
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    BoardId = newBoardId.ToString(),
                    SectionId = MapSection(t.SectionLocalId)?.ToString(),
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
                    t.StatusExpected,
                }, tx);
        }

        foreach (var w in board.Widgets)
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO widgets (id, board_id, section_id, type, grid_x, grid_y, grid_w, grid_h, config_json)
                VALUES (@Id, @BoardId, @SectionId, @Type, @GridX, @GridY, @GridW, @GridH, @ConfigJson)
                """,
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    BoardId = newBoardId.ToString(),
                    SectionId = MapSection(w.SectionLocalId)?.ToString(),
                    Type = w.Type.ToString(),
                    w.GridX,
                    w.GridY,
                    w.GridW,
                    w.GridH,
                    w.ConfigJson,
                }, tx);
        }

        await tx.CommitAsync(ct);
        return existingBoardId is not null ? ImportOutcome.Replaced : ImportOutcome.Created;
    }

    private static Task InsertSectionAsync(
        SqliteConnection conn,
        SqliteTransaction tx,
        Guid newId,
        Guid boardId,
        Guid? parentId,
        ExportedSectionDto src,
        string nowText)
        => conn.ExecuteAsync(
            """
            INSERT INTO sections (id, board_id, parent_id, name, sort_order, collapsed, created_utc, updated_utc)
            VALUES (@Id, @BoardId, @ParentId, @Name, @SortOrder, @Collapsed, @CreatedUtc, @UpdatedUtc)
            """,
            new
            {
                Id = newId.ToString(),
                BoardId = boardId.ToString(),
                ParentId = parentId?.ToString(),
                src.Name,
                src.SortOrder,
                Collapsed = src.Collapsed ? 1 : 0,
                CreatedUtc = nowText,
                UpdatedUtc = nowText,
            }, tx);
}
