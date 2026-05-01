namespace Homeboard.Boards.Entities;

public sealed record Board
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
    public int SortOrder { get; init; }
    public int GridColumns { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime UpdatedUtc { get; init; }
}
