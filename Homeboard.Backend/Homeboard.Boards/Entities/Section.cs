namespace Homeboard.Boards.Entities;

public sealed record Section
{
    public Guid Id { get; init; }
    public Guid BoardId { get; init; }
    public Guid? ParentId { get; init; }
    public string? Name { get; init; }
    public int SortOrder { get; init; }
    public bool Collapsed { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime UpdatedUtc { get; init; }
}
