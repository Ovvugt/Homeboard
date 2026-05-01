namespace Homeboard.Status.Entities;

public enum StatusValue { Unknown, Up, Down }

public sealed record TileStatusSnapshot
{
    public Guid TileId { get; init; }
    public StatusValue Status { get; init; }
    public DateTime LastCheckedUtc { get; init; }
    public DateTime? LastUpUtc { get; init; }
    public DateTime? LastDownUtc { get; init; }
    public int? ResponseTimeMs { get; init; }
    public string? Note { get; init; }
}
