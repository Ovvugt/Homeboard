using System.Diagnostics;
using System.Net.Sockets;
using Homeboard.Boards.Entities;
using Homeboard.Status.Entities;

namespace Homeboard.Status.Services;

public interface IStatusChecker
{
    Task<TileStatusSnapshot> CheckAsync(Tile tile, TileStatusSnapshot? previous, CancellationToken ct);
}

public sealed class StatusChecker(IHttpClientFactory http, TimeProvider time) : IStatusChecker
{
    public async Task<TileStatusSnapshot> CheckAsync(Tile tile, TileStatusSnapshot? previous, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        StatusValue status;
        string? note = null;
        int? responseMs = null;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(tile.StatusTimeout);

        try
        {
            switch (tile.StatusType)
            {
                case TileStatusType.HttpHead:
                case TileStatusType.HttpGet:
                {
                    var client = http.CreateClient("status");
                    client.Timeout = TimeSpan.FromMilliseconds(tile.StatusTimeout);
                    var method = tile.StatusType == TileStatusType.HttpHead ? HttpMethod.Head : HttpMethod.Get;
                    var target = tile.StatusTarget ?? tile.Url;
                    using var req = new HttpRequestMessage(method, target);
                    using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
                    var ok = tile.StatusExpected.HasValue
                        ? (int)resp.StatusCode == tile.StatusExpected.Value
                        : resp.IsSuccessStatusCode;
                    status = ok ? StatusValue.Up : StatusValue.Down;
                    note = ok ? null : $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}";
                    break;
                }
                case TileStatusType.Tcp:
                {
                    var target = tile.StatusTarget ?? "";
                    var (host, port) = ParseHostPort(target);
                    using var tcp = new TcpClient();
                    await tcp.ConnectAsync(host, port, timeoutCts.Token);
                    status = tcp.Connected ? StatusValue.Up : StatusValue.Down;
                    break;
                }
                default:
                    status = StatusValue.Unknown;
                    break;
            }
            responseMs = (int)sw.ElapsedMilliseconds;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            status = StatusValue.Down;
            note = "Timeout";
        }
        catch (Exception ex)
        {
            status = StatusValue.Down;
            note = ex.Message;
        }

        var now = time.GetUtcNow().UtcDateTime;
        return new TileStatusSnapshot
        {
            TileId = tile.Id,
            Status = status,
            LastCheckedUtc = now,
            LastUpUtc = status == StatusValue.Up ? now : previous?.LastUpUtc,
            LastDownUtc = status == StatusValue.Down ? now : previous?.LastDownUtc,
            ResponseTimeMs = responseMs,
            Note = note,
        };
    }

    private static (string Host, int Port) ParseHostPort(string target)
    {
        var idx = target.LastIndexOf(':');
        if (idx <= 0) throw new FormatException($"Expected host:port, got '{target}'.");
        var host = target[..idx];
        var port = int.Parse(target[(idx + 1)..]);
        return (host, port);
    }
}
