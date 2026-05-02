using Homeboard.Icons.Entities;
using Homeboard.Icons.Repositories;

namespace Homeboard.Icons.Services;

public interface IIconResolver
{
    Task<IconCacheEntry?> GetForUrlAsync(string url, CancellationToken ct);
    bool TryGetOrigin(string url, out string origin);
}

public sealed class IconResolver(
    IIconRepository repo,
    IIconFetcher fetcher,
    TimeProvider time) : IIconResolver
{
    private static readonly TimeSpan FreshFor = TimeSpan.FromDays(30);
    private static readonly TimeSpan FailureCooldown = TimeSpan.FromHours(6);

    public bool TryGetOrigin(string url, out string origin)
    {
        origin = "";
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;

        var host = uri.Host.ToLowerInvariant();
        if (host.Length == 0) return false;
        if (host.StartsWith("www.")) host = host[4..];

        var builder = new UriBuilder(uri.Scheme, host) { Port = uri.IsDefaultPort ? -1 : uri.Port };
        origin = builder.Uri.GetLeftPart(UriPartial.Authority).ToLowerInvariant();
        return true;
    }

    public async Task<IconCacheEntry?> GetForUrlAsync(string url, CancellationToken ct)
    {
        if (!TryGetOrigin(url, out var origin)) return null;

        var now = time.GetUtcNow().UtcDateTime;
        var cached = await repo.GetAsync(origin, ct);

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

        // Fetch from the user's full URL (path included) so apps that 401/403 at the root still resolve.
        var fetched = await fetcher.FetchAsync(url, ct);
        if (fetched is null)
        {
            await repo.UpsertAsync(new IconCacheEntry
            {
                Host = origin,
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
            Host = origin,
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
