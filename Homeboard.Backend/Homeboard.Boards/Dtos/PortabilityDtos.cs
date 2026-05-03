using Homeboard.Boards.Entities;

namespace Homeboard.Boards.Dtos;

public sealed record BoardExportDto(
    int Version,
    DateTime ExportedAt,
    IReadOnlyList<ExportedBoardDto> Boards);

public sealed record ExportedBoardDto(
    string Name,
    string Slug,
    int SortOrder,
    int GridColumns,
    IReadOnlyList<ExportedSectionDto> Sections,
    IReadOnlyList<ExportedTileDto> Tiles,
    IReadOnlyList<ExportedWidgetDto> Widgets);

public sealed record ExportedSectionDto(
    Guid LocalId,
    Guid? ParentLocalId,
    string? Name,
    int SortOrder,
    bool Collapsed);

public sealed record ExportedTileDto(
    Guid? SectionLocalId,
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

public sealed record ExportedWidgetDto(
    Guid? SectionLocalId,
    WidgetType Type,
    int GridX,
    int GridY,
    int GridW,
    int GridH,
    string ConfigJson);

public sealed record ImportResultDto(
    IReadOnlyList<string> Created,
    IReadOnlyList<string> Replaced,
    IReadOnlyList<ImportIssueDto> Skipped,
    IReadOnlyList<ImportIssueDto> Failed);

public sealed record ImportIssueDto(string Slug, string Reason);
