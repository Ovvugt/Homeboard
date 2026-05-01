using Homeboard.Icons.Entities;
using Homeboard.Icons.Repositories;

namespace Homeboard.Icons.Services;

public interface IIconResolver
{
    Task<IconCacheEntry?> GetForUrlAsync(string url, CancellationToken ct);
    bool TryGetHost(string url, out string host);
}

public sealed class IconResolver(
    IIconRepository repo,
    IIconFetcher fetcher,
    TimeProvider time) : IIconResolver
{
    private static readonly TimeSpan FreshFor = TimeSpan.FromDays(30);
    private static readonly TimeSpan FailureCooldown = TimeSpan.FromHours(6);

    public bool TryGetHost(string url, out string host)
    {
        host = "";
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
        var h = uri.Host.ToLowerInvariant();
        if (h.StartsWith("www.")) h = h[4..];
        host = h;
        return host.Length > 0;
    }

    public async Task<IconCacheEntry?> GetForUrlAsync(string url, CancellationToken ct)
    {
        if (!TryGetHost(url, out var host)) return null;

        var now = time.GetUtcNow().UtcDateTime;
        var cached = await repo.GetAsync(host, ct);

        if (cached is not null)
        {
            var age = now - cached.FetchedUtc;
            if (cached.Failed)
            {
                if (age < FailureCooldown) return null;
            }
            else if (age < FreshFor)
            {
                return cached;
            }
        }

        var fetched = await fetcher.FetchAsync(host, ct);
        if (fetched is null)
        {
            await repo.UpsertAsync(new IconCacheEntry
            {
                Host = host,
                ContentType = "",
                Bytes = [],
                SourceUrl = "",
                FetchedUtc = now,
                Failed = true,
            }, ct);
            return null;
        }

        var entry = new IconCacheEntry
        {
            Host = host,
            ContentType = fetched.ContentType,
            Bytes = fetched.Bytes,
            SourceUrl = fetched.SourceUrl,
            FetchedUtc = now,
            Failed = false,
        };
        await repo.UpsertAsync(entry, ct);
        return entry;
    }
}
