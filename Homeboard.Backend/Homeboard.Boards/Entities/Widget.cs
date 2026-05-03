namespace Homeboard.Boards.Entities;

public enum WidgetType { Clock, Weather, Minecraft }

public sealed record Widget
{
    public Guid Id { get; init; }
    public Guid BoardId { get; init; }
    public WidgetType Type { get; init; }
    public int GridX { get; init; }
    public int GridY { get; init; }
    public int GridW { get; init; }
    public int GridH { get; init; }
    public string ConfigJson { get; init; } = "{}";
}
