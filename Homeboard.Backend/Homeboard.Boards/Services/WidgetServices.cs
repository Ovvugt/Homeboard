using Homeboard.Boards.Dtos;
using Homeboard.Boards.Entities;
using Homeboard.Boards.Repositories;

namespace Homeboard.Boards.Services;

public interface IWidgetCreator
{
    Task<WidgetDto> CreateAsync(CreateWidgetDto dto, CancellationToken ct);
}

public sealed class WidgetCreator(
    IBoardRepository boards,
    ISectionRepository sections,
    IWidgetRepository widgets) : IWidgetCreator
{
    public async Task<WidgetDto> CreateAsync(CreateWidgetDto dto, CancellationToken ct)
    {
        var board = await boards.GetByIdAsync(dto.BoardId, ct)
            ?? throw new InvalidOperationException($"Board '{dto.BoardId}' not found.");
        var sectionId = await TileCreator.ResolveSectionAsync(sections, board.Id, dto.SectionId, ct);
        var widget = new Widget
        {
            Id = Guid.NewGuid(),
            BoardId = board.Id,
            SectionId = sectionId,
            Type = dto.Type,
            GridX = dto.GridX,
            GridY = dto.GridY,
            GridW = dto.GridW,
            GridH = dto.GridH,
            ConfigJson = string.IsNullOrWhiteSpace(dto.ConfigJson) ? "{}" : dto.ConfigJson
        };
        await widgets.InsertAsync(widget, ct);
        return new WidgetDto(widget.Id, widget.BoardId, widget.SectionId, widget.Type, widget.GridX, widget.GridY, widget.GridW, widget.GridH, widget.ConfigJson);
    }
}

public interface IWidgetUpdater
{
    Task<bool> UpdateAsync(Guid id, UpdateWidgetDto dto, CancellationToken ct);
}

public sealed class WidgetUpdater(IWidgetRepository widgets) : IWidgetUpdater
{
    public async Task<bool> UpdateAsync(Guid id, UpdateWidgetDto dto, CancellationToken ct)
    {
        var existing = await widgets.GetByIdAsync(id, ct);
        if (existing is null) return false;
        var updated = existing with { ConfigJson = string.IsNullOrWhiteSpace(dto.ConfigJson) ? "{}" : dto.ConfigJson };
        await widgets.UpdateAsync(updated, ct);
        return true;
    }
}

public interface IWidgetDeleter
{
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public sealed class WidgetDeleter(IWidgetRepository widgets) : IWidgetDeleter
{
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => widgets.DeleteAsync(id, ct);
}

public interface ILayoutSaver
{
    Task SaveAsync(Guid boardId, SaveLayoutDto dto, CancellationToken ct);
}

public sealed class LayoutSaver(ILayoutRepository layouts) : ILayoutSaver
{
    public Task SaveAsync(Guid boardId, SaveLayoutDto dto, CancellationToken ct) =>
        layouts.SaveAsync(dto.Items, ct);
}
