using Homeboard.Boards.Dtos;
using Homeboard.Boards.Entities;
using Homeboard.Boards.Repositories;

namespace Homeboard.Boards.Services;

public interface ITileCreator
{
    Task<TileDto> CreateAsync(CreateTileDto dto, CancellationToken ct);
}

public sealed class TileCreator(
    IBoardRepository boards,
    ISectionRepository sections,
    ITileRepository tiles) : ITileCreator
{
    public async Task<TileDto> CreateAsync(CreateTileDto dto, CancellationToken ct)
    {
        var board = await boards.GetByIdAsync(dto.BoardId, ct)
            ?? throw new InvalidOperationException($"Board '{dto.BoardId}' not found.");

        var sectionId = await ResolveSectionAsync(sections, board.Id, dto.SectionId, ct);

        var tile = new Tile
        {
            Id = Guid.NewGuid(),
            BoardId = board.Id,
            SectionId = sectionId,
            Name = dto.Name.Trim(),
            Url = dto.Url.Trim(),
            IconUrl = string.IsNullOrWhiteSpace(dto.IconUrl) ? null : dto.IconUrl.Trim(),
            IconKind = dto.IconKind,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            Color = string.IsNullOrWhiteSpace(dto.Color) ? null : dto.Color.Trim(),
            GridX = dto.GridX,
            GridY = dto.GridY,
            GridW = dto.GridW,
            GridH = dto.GridH,
            StatusType = dto.StatusType,
            StatusTarget = string.IsNullOrWhiteSpace(dto.StatusTarget) ? null : dto.StatusTarget.Trim(),
            StatusInterval = dto.StatusInterval ?? 60,
            StatusTimeout = dto.StatusTimeout ?? 5000,
            StatusExpected = dto.StatusExpected
        };
        await tiles.InsertAsync(tile, ct);
        return new TileDto(
            tile.Id, tile.BoardId, tile.SectionId, tile.Name, tile.Url, tile.IconUrl, tile.IconKind,
            tile.Description, tile.Color, tile.GridX, tile.GridY, tile.GridW, tile.GridH,
            tile.StatusType, tile.StatusTarget, tile.StatusInterval, tile.StatusTimeout, tile.StatusExpected);
    }

    internal static async Task<Guid?> ResolveSectionAsync(
        ISectionRepository sections, Guid boardId, Guid? requested, CancellationToken ct)
    {
        if (requested.HasValue)
        {
            var section = await sections.GetByIdAsync(requested.Value, ct)
                ?? throw new InvalidOperationException($"Section '{requested}' not found.");
            if (section.BoardId != boardId)
            {
                throw new InvalidOperationException("Section does not belong to the target board.");
            }
            return section.Id;
        }

        var root = await sections.GetRootForBoardAsync(boardId, ct);
        return root?.Id;
    }
}

public interface ITileUpdater
{
    Task<bool> UpdateAsync(Guid id, UpdateTileDto dto, CancellationToken ct);
}

public sealed class TileUpdater(ITileRepository tiles) : ITileUpdater
{
    public async Task<bool> UpdateAsync(Guid id, UpdateTileDto dto, CancellationToken ct)
    {
        var existing = await tiles.GetByIdAsync(id, ct);
        if (existing is null) return false;
        var updated = existing with
        {
            Name = dto.Name.Trim(),
            Url = dto.Url.Trim(),
            IconUrl = string.IsNullOrWhiteSpace(dto.IconUrl) ? null : dto.IconUrl.Trim(),
            IconKind = dto.IconKind,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            Color = string.IsNullOrWhiteSpace(dto.Color) ? null : dto.Color.Trim(),
            StatusType = dto.StatusType,
            StatusTarget = string.IsNullOrWhiteSpace(dto.StatusTarget) ? null : dto.StatusTarget.Trim(),
            StatusInterval = dto.StatusInterval,
            StatusTimeout = dto.StatusTimeout,
            StatusExpected = dto.StatusExpected
        };
        await tiles.UpdateAsync(updated, ct);
        return true;
    }
}

public interface ITileDeleter
{
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public sealed class TileDeleter(ITileRepository tiles) : ITileDeleter
{
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => tiles.DeleteAsync(id, ct);
}
