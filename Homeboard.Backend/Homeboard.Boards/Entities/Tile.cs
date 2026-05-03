namespace Homeboard.Boards.Entities;

public enum TileIconKind { Url, Initials, Builtin }

public enum TileStatusType { None, HttpHead, HttpGet, Tcp }

public sealed record Tile
{
    public Guid Id { get; init; }
    public Guid BoardId { get; init; }
    public Guid? SectionId { get; init; }
    public string Name { get; init; } = "";
    public string Url { get; init; } = "";
    public string? IconUrl { get; init; }
    public TileIconKind IconKind { get; init; }
    public string? Description { get; init; }
    public string? Color { get; init; }
    public int GridX { get; init; }
    public int GridY { get; init; }
    public int GridW { get; init; }
    public int GridH { get; init; }
    public TileStatusType StatusType { get; init; }
    public string? StatusTarget { get; init; }
    public int StatusInterval { get; init; }
    public int StatusTimeout { get; init; }
    public int? StatusExpected { get; init; }
}
