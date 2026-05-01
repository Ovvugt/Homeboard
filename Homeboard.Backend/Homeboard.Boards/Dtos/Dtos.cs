using Homeboard.Boards.Entities;

namespace Homeboard.Boards.Dtos;

public sealed record BoardSummaryDto(Guid Id, string Name, string Slug, int SortOrder, int GridColumns);

public sealed record BoardDetailDto(
    Guid Id,
    string Name,
    string Slug,
    int SortOrder,
    int GridColumns,
    IReadOnlyList<TileDto> Tiles,
    IReadOnlyList<WidgetDto> Widgets);

public sealed record TileDto(
    Guid Id,
    Guid BoardId,
    string Name,
    string Url,
    string? IconUrl,
    TileIconKind IconKind,
    string? Description,
    string? Color,
    int GridX,
    int GridY,
    int GridW,
    int GridH,
    TileStatusType StatusType,
    string? StatusTarget,
    int StatusInterval,
    int StatusTimeout,
    int? StatusExpected);

public sealed record WidgetDto(
    Guid Id,
    Guid BoardId,
    WidgetType Type,
    int GridX,
    int GridY,
    int GridW,
    int GridH,
    string ConfigJson);

public sealed record CreateBoardDto(string Name, string Slug, int? GridColumns);

public sealed record UpdateBoardDto(string Name, string Slug, int GridColumns);

public sealed record CreateTileDto(
    Guid BoardId,
    string Name,
    string Url,
    string? IconUrl,
    TileIconKind IconKind,
    string? Description,
    string? Color,
    int GridX,
    int GridY,
    int GridW,
    int GridH,
    TileStatusType StatusType,
    string? StatusTarget,
    int? StatusInterval,
    int? StatusTimeout,
    int? StatusExpected);

public sealed record UpdateTileDto(
    string Name,
    string Url,
    string? IconUrl,
    TileIconKind IconKind,
    string? Description,
    string? Color,
    TileStatusType StatusType,
    string? StatusTarget,
    int StatusInterval,
    int StatusTimeout,
    int? StatusExpected);

public sealed record CreateWidgetDto(
    Guid BoardId,
    WidgetType Type,
    int GridX,
    int GridY,
    int GridW,
    int GridH,
    string? ConfigJson);

public sealed record UpdateWidgetDto(string ConfigJson);

public sealed record LayoutItemDto(Guid Id, LayoutItemKind Kind, int GridX, int GridY, int GridW, int GridH);

public enum LayoutItemKind { Tile, Widget }

public sealed record SaveLayoutDto(IReadOnlyList<LayoutItemDto> Items);
