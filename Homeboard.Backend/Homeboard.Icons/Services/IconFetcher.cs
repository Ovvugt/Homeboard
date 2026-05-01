using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Homeboard.Icons.Services;

public sealed record FetchedIcon(byte[] Bytes, string ContentType, string SourceUrl);

public interface IIconFetcher
{
    Task<FetchedIcon?> FetchAsync(string host, CancellationToken ct);
}

public sealed partial class IconFetcher(IHttpClientFactory http, ILogger<IconFetcher> logger) : IIconFetcher
{
    private const int MaxBytes = 512 * 1024;        // 512 KB cap per icon
    private const int HtmlScanBytes = 64 * 1024;    // only scan first 64 KB of HTML for <link>

    public async Task<FetchedIcon?> FetchAsync(string host, CancellationToken ct)
    {
        var client = http.CreateClient("icons");
        client.Timeout = TimeSpan.FromSeconds(8);

        // 1) Try /favicon.ico directly
        var direct = await TryDownloadImageAsync(client, $"https://{host}/favicon.ico", ct);
        if (direct is not null) return direct;

        // 2) Try parsing the homepage for <link rel="icon" ...>
        var parsed = await TryParseHomepageIconAsync(client, host, ct);
        if (parsed is not null) return parsed;

        // 3) Fallback to Google's public S2 favicon service
        var s2 = await TryDownloadImageAsync(client, $"https://www.google.com/s2/favicons?domain={host}&sz=64", ct);
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

    private async Task<FetchedIcon?> TryParseHomepageIconAsync(HttpClient client, string host, CancellationToken ct)
    {
        try
        {
            var origin = $"https://{host}";
            using var resp = await client.GetAsync(origin, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!resp.IsSuccessStatusCode) return null;
            var contentType = resp.Content.Headers.ContentType?.MediaType ?? "";
            if (!contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase)) return null;

            var html = await ReadStringCappedAsync(resp, HtmlScanBytes, ct);
            foreach (var href in ExtractIconHrefs(html))
            {
                var resolved = ResolveUrl(origin, href);
                if (resolved is null) continue;
                var icon = await TryDownloadImageAsync(client, resolved, ct);
                if (icon is not null) return icon;
            }
            return null;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Homepage parse failed for {Host}", host);
            return null;
        }
    }

    private static IEnumerable<string> ExtractIconHrefs(string html)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in LinkTagRegex().Matches(html))
        {
            var rel = m.Groups["rel"].Value.ToLowerInvariant();
            if (!rel.Contains("icon")) continue;
            var href = m.Groups["href"].Value;
            if (string.IsNullOrWhiteSpace(href)) continue;
            if (seen.Add(href)) yield return href;
        }
    }

    private static string? ResolveUrl(string origin, string href)
    {
        if (Uri.TryCreate(href, UriKind.Absolute, out var abs))
        {
            return (abs.Scheme == Uri.UriSchemeHttp || abs.Scheme == Uri.UriSchemeHttps) ? abs.ToString() : null;
        }
        if (Uri.TryCreate(new Uri(origin), href, out var rel))
        {
            return rel.ToString();
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

    [GeneratedRegex("""<link\s+(?=[^>]*\brel=["'](?<rel>[^"']+)["'])(?=[^>]*\bhref=["'](?<href>[^"']+)["'])[^>]*>""", RegexOptions.IgnoreCase)]
    private static partial Regex LinkTagRegex();
}
