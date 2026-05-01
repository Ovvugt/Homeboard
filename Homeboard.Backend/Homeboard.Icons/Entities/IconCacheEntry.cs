namespace Homeboard.Icons.Entities;

public sealed record IconCacheEntry
{
    public string Host { get; init; } = "";
    public string ContentType { get; init; } = "";
    public byte[] Bytes { get; init; } = [];
    public string SourceUrl { get; init; } = "";
    public DateTime FetchedUtc { get; init; }
    public bool Failed { get; init; }
}
