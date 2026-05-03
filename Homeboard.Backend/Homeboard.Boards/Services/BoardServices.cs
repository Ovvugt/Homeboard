using Homeboard.Boards.Dtos;
using Homeboard.Boards.Entities;
using Homeboard.Boards.Repositories;

namespace Homeboard.Boards.Services;

public interface IBoardReader
{
    Task<IReadOnlyList<BoardSummaryDto>> ListAsync(CancellationToken ct);
    Task<BoardDetailDto?> GetBySlugAsync(string slug, CancellationToken ct);
}

public sealed class BoardReader(
    IBoardRepository boards,
    ITileRepository tiles,
    IWidgetRepository widgets,
    ISectionRepository sections) : IBoardReader
{
    public async Task<IReadOnlyList<BoardSummaryDto>> ListAsync(CancellationToken ct)
    {
        var rows = await boards.ListAsync(ct);
        return rows.Select(b => new BoardSummaryDto(b.Id, b.Name, b.Slug, b.SortOrder, b.GridColumns)).ToList();
    }

    public async Task<BoardDetailDto?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        var board = await boards.GetBySlugAsync(slug, ct);
        if (board is null) return null;
        var sectionList = await sections.ListByBoardAsync(board.Id, ct);
        var tileList = await tiles.ListByBoardAsync(board.Id, ct);
        var widgetList = await widgets.ListByBoardAsync(board.Id, ct);
        return new BoardDetailDto(
            board.Id, board.Name, board.Slug, board.SortOrder, board.GridColumns,
            sectionList.Select(BoardMapper.ToDto).ToList(),
            tileList.Select(BoardMapper.ToDto).ToList(),
            widgetList.Select(BoardMapper.ToDto).ToList());
    }
}

public interface IBoardCreator
{
    Task<BoardSummaryDto> CreateAsync(CreateBoardDto dto, CancellationToken ct);
}

public sealed class BoardCreator(
    IBoardRepository boards,
    ISectionRepository sections,
    TimeProvider time) : IBoardCreator
{
    public async Task<BoardSummaryDto> CreateAsync(CreateBoardDto dto, CancellationToken ct)
    {
        var slug = SlugNormalizer.Normalize(string.IsNullOrWhiteSpace(dto.Slug) ? dto.Name : dto.Slug);
        if (await boards.GetBySlugAsync(slug, ct) is not null)
        {
            throw new InvalidOperationException($"A board with slug '{slug}' already exists.");
        }
        var now = time.GetUtcNow().UtcDateTime;
        var existing = await boards.ListAsync(ct);
        var nextSort = existing.Count == 0 ? 0 : existing.Max(b => b.SortOrder) + 1;
        var board = new Board
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Slug = slug,
            SortOrder = nextSort,
            GridColumns = dto.GridColumns ?? 12,
            CreatedUtc = now,
            UpdatedUtc = now
        };
        await boards.InsertAsync(board, ct);

        // Every board gets an implicit unnamed root section.
        await sections.InsertAsync(new Section
        {
            Id = Guid.NewGuid(),
            BoardId = board.Id,
            ParentId = null,
            Name = null,
            SortOrder = 0,
            Collapsed = false,
            CreatedUtc = now,
            UpdatedUtc = now
        }, ct);

        return new BoardSummaryDto(board.Id, board.Name, board.Slug, board.SortOrder, board.GridColumns);
    }
}

public interface IBoardUpdater
{
    Task<bool> UpdateAsync(Guid id, UpdateBoardDto dto, CancellationToken ct);
}

public sealed class BoardUpdater(IBoardRepository boards, TimeProvider time) : IBoardUpdater
{
    public async Task<bool> UpdateAsync(Guid id, UpdateBoardDto dto, CancellationToken ct)
    {
        var board = await boards.GetByIdAsync(id, ct);
        if (board is null) return false;
        var slug = SlugNormalizer.Normalize(dto.Slug);
        if (slug != board.Slug && await boards.GetBySlugAsync(slug, ct) is not null)
        {
            throw new InvalidOperationException($"A board with slug '{slug}' already exists.");
        }
        var updated = board with
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            GridColumns = dto.GridColumns,
            UpdatedUtc = time.GetUtcNow().UtcDateTime
        };
        await boards.UpdateAsync(updated, ct);
        return true;
    }
}

public interface IBoardDeleter
{
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public sealed class BoardDeleter(IBoardRepository boards) : IBoardDeleter
{
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => boards.DeleteAsync(id, ct);
}

internal static class BoardMapper
{
    public static TileDto ToDto(Tile t) => new(
        t.Id, t.BoardId, t.SectionId, t.Name, t.Url, t.IconUrl, t.IconKind, t.Description, t.Color,
        t.GridX, t.GridY, t.GridW, t.GridH,
        t.StatusType, t.StatusTarget, t.StatusInterval, t.StatusTimeout, t.StatusExpected);

    public static WidgetDto ToDto(Widget w) =>
        new(w.Id, w.BoardId, w.SectionId, w.Type, w.GridX, w.GridY, w.GridW, w.GridH, w.ConfigJson);

    public static SectionDto ToDto(Section s) =>
        new(s.Id, s.BoardId, s.ParentId, s.Name, s.SortOrder, s.Collapsed);
}
