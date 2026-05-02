using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Homeboard.Icons.Services;

public sealed record FetchedIcon(byte[] Bytes, string ContentType, string SourceUrl);

public interface IIconFetcher
{
    Task<FetchedIcon?> FetchAsync(string pageUrl, CancellationToken ct);
}

public sealed partial class IconFetcher(IHttpClientFactory http, ILogger<IconFetcher> logger) : IIconFetcher
{
    private const int MaxBytes = 512 * 1024;        // 512 KB cap per icon
    private const int HtmlScanBytes = 128 * 1024;   // scan first 128 KB of HTML for <link>

    public async Task<FetchedIcon?> FetchAsync(string pageUrl, CancellationToken ct)
    {
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var pageUri)) return null;
        var originUri = new Uri(pageUri.GetLeftPart(UriPartial.Authority));

        var client = http.CreateClient("icons");
        client.Timeout = TimeSpan.FromSeconds(8);

        // 1) Parse the user's path for <link rel="icon" ...> first.
        //    The root often 401/403s on self-hosted apps (e.g. dns01.home → 403, dns01.home/admin → login page).
        var parsed = await TryParseHomepageIconAsync(client, pageUri, ct);
        if (parsed is not null) return parsed;

        // 2) If the user gave us a non-root path, also try the origin root in case it works.
        if (pageUri.AbsolutePath.Length > 1)
        {
            var rootParsed = await TryParseHomepageIconAsync(client, originUri, ct);
            if (rootParsed is not null) return rootParsed;
        }

        // 3) Fall back to /favicon.ico at the origin root.
        var direct = await TryDownloadImageAsync(client, new Uri(originUri, "/favicon.ico").ToString(), ct);
        if (direct is not null) return direct;

        // 4) Fall back to Google's public S2 favicon service (uses host only, ignores port).
        var s2 = await TryDownloadImageAsync(client, $"https://www.google.com/s2/favicons?domain={originUri.Host}&sz=64", ct);
        if (s2 is not null) return s2;

        return null;
    }

    private async Task<FetchedIcon?> TryDownloadImageAsync(HttpClient client, string url, CancellationToken ct)
    {
        try
        {
            using var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!resp.IsSuccessStatusCode) return null;
            var contentType = resp.Content.Headers.ContentType?.MediaType ?? "";
            if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return null;
            var bytes = await ReadCappedAsync(resp, ct);
            if (bytes is null) return null;
            return new FetchedIcon(bytes, contentType, url);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Icon download failed: {Url}", url);
            return null;
        }
    }

    private async Task<FetchedIcon?> TryParseHomepageIconAsync(HttpClient client, Uri origin, CancellationToken ct)
    {
        try
        {
            using var resp = await client.GetAsync(origin, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!resp.IsSuccessStatusCode) return null;
            var contentType = resp.Content.Headers.ContentType?.MediaType ?? "";
            if (!contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase)) return null;

            // Resolve against the final URL after redirects (e.g. host → www.host, or /login).
            var baseUri = resp.RequestMessage?.RequestUri ?? origin;
            var html = await ReadStringCappedAsync(resp, HtmlScanBytes, ct);

            foreach (var href in RankIconHrefs(html))
            {
                var resolved = ResolveUrl(baseUri, href);
                if (resolved is null) continue;
                var icon = await TryDownloadImageAsync(client, resolved, ct);
                if (icon is not null) return icon;
            }
            return null;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Homepage parse failed for {Origin}", origin);
            return null;
        }
    }

    private sealed record IconCandidate(string Href, int Score);

    private static IEnumerable<string> RankIconHrefs(string html)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<IconCandidate>();

        foreach (Match link in LinkTagRegex().Matches(html))
        {
            var attrs = ParseAttrs(link.Value);
            if (!attrs.TryGetValue("rel", out var rel) || !attrs.TryGetValue("href", out var href)) continue;
            if (string.IsNullOrWhiteSpace(href)) continue;

            var rels = rel.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.ToLowerInvariant())
                .ToHashSet();
            if (!rels.Any(r => r is "icon" or "shortcut" or "apple-touch-icon" or "apple-touch-icon-precomposed" or "mask-icon")) continue;
            // "shortcut" alone isn't an icon; needs to pair with "icon" (rel="shortcut icon").
            if (rels.Contains("shortcut") && !rels.Contains("icon")) continue;

            if (!seen.Add(href)) continue;

            var score = ScoreIcon(rels, attrs);
            candidates.Add(new IconCandidate(href, score));
        }

        return candidates.OrderByDescending(c => c.Score).Select(c => c.Href);
    }

    private static int ScoreIcon(HashSet<string> rels, Dictionary<string, string> attrs)
    {
        var score = 0;

        // Prefer apple-touch-icon (typically 180x180 PNG) over generic icon (often 16x16 .ico).
        if (rels.Contains("apple-touch-icon-precomposed")) score += 50;
        if (rels.Contains("apple-touch-icon")) score += 40;
        if (rels.Contains("icon")) score += 20;
        if (rels.Contains("mask-icon")) score += 5;

        // Prefer modern image formats over .ico when declared via type=.
        if (attrs.TryGetValue("type", out var type))
        {
            type = type.ToLowerInvariant();
            if (type.Contains("png")) score += 15;
            else if (type.Contains("svg")) score += 25;
            else if (type.Contains("webp")) score += 10;
            else if (type.Contains("x-icon") || type.Contains("vnd.microsoft.icon")) score += 0;
        }

        // Use sizes hint when present (e.g. "32x32", "any").
        if (attrs.TryGetValue("sizes", out var sizes))
        {
            sizes = sizes.ToLowerInvariant();
            if (sizes.Contains("any")) score += 30;
            else
            {
                var biggest = SizesRegex().Matches(sizes)
                    .Select(m => int.TryParse(m.Groups[1].Value, out var n) ? n : 0)
                    .DefaultIfEmpty(0)
                    .Max();
                score += Math.Min(biggest, 512) / 8;
            }
        }

        return score;
    }

    private static Dictionary<string, string> ParseAttrs(string tag)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in AttrRegex().Matches(tag))
        {
            var name = m.Groups["n"].Value;
            var value = m.Groups["dq"].Success ? m.Groups["dq"].Value
                      : m.Groups["sq"].Success ? m.Groups["sq"].Value
                      : m.Groups["uq"].Value;
            dict[name] = value;
        }
        return dict;
    }

    private static string? ResolveUrl(Uri baseUri, string href)
    {
        href = href.Trim();
        if (href.StartsWith("//", StringComparison.Ordinal))
        {
            href = baseUri.Scheme + ":" + href;
        }
        if (Uri.TryCreate(href, UriKind.Absolute, out var abs))
        {
            return (abs.Scheme == Uri.UriSchemeHttp || abs.Scheme == Uri.UriSchemeHttps) ? abs.ToString() : null;
        }
        if (Uri.TryCreate(baseUri, href, out var rel))
        {
            return (rel.Scheme == Uri.UriSchemeHttp || rel.Scheme == Uri.UriSchemeHttps) ? rel.ToString() : null;
        }
        return null;
    }

    private static async Task<byte[]?> ReadCappedAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var ms = new MemoryStream();
        var buf = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(buf, ct)) > 0)
        {
            if (ms.Length + read > MaxBytes) return null;
            ms.Write(buf, 0, read);
        }
        return ms.Length == 0 ? null : ms.ToArray();
    }

    private static async Task<string> ReadStringCappedAsync(HttpResponseMessage resp, int max, CancellationToken ct)
    {
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var ms = new MemoryStream();
        var buf = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(buf, ct)) > 0 && ms.Length < max)
        {
            ms.Write(buf, 0, read);
        }
        return System.Text.Encoding.UTF8.GetString(ms.ToArray());
    }

    [GeneratedRegex("<link\\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex LinkTagRegex();

    [GeneratedRegex("""(?<n>[A-Za-z_][\w:-]*)\s*=\s*(?:"(?<dq>[^"]*)"|'(?<sq>[^']*)'|(?<uq>[^\s"'>]+))""", RegexOptions.IgnoreCase)]
    private static partial Regex AttrRegex();

    [GeneratedRegex("""(\d+)x\d+""", RegexOptions.IgnoreCase)]
    private static partial Regex SizesRegex();
}
