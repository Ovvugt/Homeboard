using Homeboard.Boards.Dtos;
using Homeboard.Boards.Entities;
using Homeboard.Boards.Repositories;

namespace Homeboard.Boards.Services;

public interface ISectionCreator
{
    Task<SectionDto> CreateAsync(CreateSectionDto dto, CancellationToken ct);
}

public sealed class SectionCreator(
    IBoardRepository boards,
    ISectionRepository sections,
    TimeProvider time) : ISectionCreator
{
    public async Task<SectionDto> CreateAsync(CreateSectionDto dto, CancellationToken ct)
    {
        var board = await boards.GetByIdAsync(dto.BoardId, ct)
            ?? throw new InvalidOperationException($"Board '{dto.BoardId}' not found.");

        if (dto.ParentId.HasValue)
        {
            var parent = await sections.GetByIdAsync(dto.ParentId.Value, ct)
                ?? throw new InvalidOperationException($"Parent section '{dto.ParentId}' not found.");
            if (parent.BoardId != board.Id)
            {
                throw new InvalidOperationException("Parent section belongs to a different board.");
            }
        }
        else
        {
            // A board can have only one root section; subsequent ones are nested under root.
            var existingRoot = await sections.GetRootForBoardAsync(board.Id, ct);
            if (existingRoot is not null)
            {
                throw new InvalidOperationException("Board already has a root section. Provide a parentId.");
            }
        }

        var siblings = (await sections.ListByBoardAsync(board.Id, ct))
            .Where(s => s.ParentId == dto.ParentId).ToList();
        var nextSort = siblings.Count == 0 ? 0 : siblings.Max(s => s.SortOrder) + 1;

        var now = time.GetUtcNow().UtcDateTime;
        var section = new Section
        {
            Id = Guid.NewGuid(),
            BoardId = board.Id,
            ParentId = dto.ParentId,
            Name = string.IsNullOrWhiteSpace(dto.Name) ? null : dto.Name.Trim(),
            SortOrder = nextSort,
            Collapsed = false,
            CreatedUtc = now,
            UpdatedUtc = now
        };
        await sections.InsertAsync(section, ct);
        return new SectionDto(section.Id, section.BoardId, section.ParentId, section.Name, section.SortOrder, section.Collapsed);
    }
}

public interface ISectionUpdater
{
    Task<bool> UpdateAsync(Guid id, UpdateSectionDto dto, CancellationToken ct);
}

public sealed class SectionUpdater(ISectionRepository sections, TimeProvider time) : ISectionUpdater
{
    public async Task<bool> UpdateAsync(Guid id, UpdateSectionDto dto, CancellationToken ct)
    {
        var existing = await sections.GetByIdAsync(id, ct);
        if (existing is null) return false;

        // Reparenting validation: target must belong to same board, must not create a cycle, and the
        // root section's parent_id must remain NULL.
        if (existing.ParentId is null && dto.ParentId is not null)
        {
            throw new InvalidOperationException("The root section cannot be re-parented.");
        }

        if (dto.ParentId.HasValue && dto.ParentId != existing.ParentId)
        {
            if (dto.ParentId == id)
            {
                throw new InvalidOperationException("A section cannot be its own parent.");
            }

            var all = await sections.ListByBoardAsync(existing.BoardId, ct);
            var byId = all.ToDictionary(s => s.Id);

            if (!byId.TryGetValue(dto.ParentId.Value, out var parent) || parent.BoardId != existing.BoardId)
            {
                throw new InvalidOperationException("Parent section must belong to the same board.");
            }

            // Walk ancestors of the proposed parent — if we hit `id` we have a cycle.
            var cursor = parent;
            while (cursor.ParentId is not null)
            {
                if (cursor.ParentId == id)
                {
                    throw new InvalidOperationException("Reparenting would create a cycle.");
                }
                if (!byId.TryGetValue(cursor.ParentId.Value, out var next)) break;
                cursor = next;
            }
        }

        var updated = existing with
        {
            Name = string.IsNullOrWhiteSpace(dto.Name) ? null : dto.Name.Trim(),
            ParentId = existing.ParentId is null ? null : dto.ParentId,
            SortOrder = dto.SortOrder,
            Collapsed = dto.Collapsed,
            UpdatedUtc = time.GetUtcNow().UtcDateTime
        };
        await sections.UpdateAsync(updated, ct);
        return true;
    }
}

public interface ISectionDeleter
{
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public sealed class SectionDeleter(ISectionRepository sections) : ISectionDeleter
{
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var existing = await sections.GetByIdAsync(id, ct);
        if (existing is null) return false;
        if (existing.ParentId is null)
        {
            throw new InvalidOperationException("The root section cannot be deleted.");
        }

        var root = await sections.GetRootForBoardAsync(existing.BoardId, ct)
            ?? throw new InvalidOperationException("Board has no root section to re-home items into.");

        return await sections.DeleteAsync(id, root.Id, ct);
    }
}
