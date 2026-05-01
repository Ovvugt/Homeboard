using Dapper;
using Homeboard.Core.Data;
using Homeboard.Icons.Entities;

namespace Homeboard.Icons.Repositories;

public interface IIconRepository
{
    Task<IconCacheEntry?> GetAsync(string host, CancellationToken ct);
    Task UpsertAsync(IconCacheEntry entry, CancellationToken ct);
}

public sealed class IconRepository(ISqliteConnectionFactory factory) : IIconRepository
{
    private const string SelectColumns = """
        host AS Host,
        content_type AS ContentType,
        bytes AS Bytes,
        source_url AS SourceUrl,
        fetched_utc AS FetchedUtc,
        failed AS Failed
        """;

    public async Task<IconCacheEntry?> GetAsync(string host, CancellationToken ct)
    {
        await using var conn = factory.Create();
        return await conn.QuerySingleOrDefaultAsync<IconCacheEntry>(
            $"SELECT {SelectColumns} FROM icon_cache WHERE host = @host",
            new { host });
    }

    public async Task UpsertAsync(IconCacheEntry entry, CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.ExecuteAsync(
            """
            INSERT INTO icon_cache (host, content_type, bytes, source_url, fetched_utc, failed)
            VALUES (@Host, @ContentType, @Bytes, @SourceUrl, @FetchedUtc, @Failed)
            ON CONFLICT(host) DO UPDATE SET
                content_type = excluded.content_type,
                bytes = excluded.bytes,
                source_url = excluded.source_url,
                fetched_utc = excluded.fetched_utc,
                failed = excluded.failed
            """,
            new
            {
                entry.Host,
                entry.ContentType,
                entry.Bytes,
                entry.SourceUrl,
                FetchedUtc = entry.FetchedUtc.ToString("O"),
                Failed = entry.Failed ? 1 : 0,
            });
    }
}
