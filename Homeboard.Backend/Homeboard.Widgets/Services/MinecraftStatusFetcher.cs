using System.Buffers.Binary;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Homeboard.Widgets.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Homeboard.Widgets.Services;

public interface IMinecraftStatusFetcher
{
    Task<MinecraftStatusDto> GetStatusAsync(string host, int port, CancellationToken ct);
}

public sealed class MinecraftStatusFetcher(
    IMemoryCache cache,
    IConfiguration config,
    ILogger<MinecraftStatusFetcher> logger) : IMinecraftStatusFetcher
{
    private const int DefaultPort = 25565;
    private const int ProtocolVersionAny = -1;
    private const int MaxPayloadBytes = 2 * 1024 * 1024;

    public async Task<MinecraftStatusDto> GetStatusAsync(string host, int port, CancellationToken ct)
    {
        var trimmedHost = host.Trim();
        var resolvedPort = port <= 0 ? DefaultPort : port;
        var cacheKey = $"minecraft:{trimmedHost}:{resolvedPort}";
        if (cache.TryGetValue<MinecraftStatusDto>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var connectTimeoutMs = config.GetValue<int?>("Minecraft:ConnectTimeoutMs") ?? 4000;
        var readTimeoutMs = config.GetValue<int?>("Minecraft:ReadTimeoutMs") ?? 4000;
        var cacheSeconds = config.GetValue<int?>("Minecraft:CacheSeconds") ?? 30;
        var errorCacheSeconds = config.GetValue<int?>("Minecraft:ErrorCacheSeconds") ?? 15;

        MinecraftStatusDto dto;
        try
        {
            dto = await FetchAsync(trimmedHost, resolvedPort, connectTimeoutMs, readTimeoutMs, ct);
            cache.Set(cacheKey, dto, TimeSpan.FromSeconds(cacheSeconds));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Minecraft status failed for {Host}:{Port}", trimmedHost, resolvedPort);
            dto = new MinecraftStatusDto(
                trimmedHost, resolvedPort, false,
                null, null, null, null, null, null, null, null,
                ShortError(ex), DateTime.UtcNow);
            cache.Set(cacheKey, dto, TimeSpan.FromSeconds(errorCacheSeconds));
        }

        return dto;
    }

    private static async Task<MinecraftStatusDto> FetchAsync(
        string host, int port, int connectTimeoutMs, int readTimeoutMs, CancellationToken ct)
    {
        using var tcp = new TcpClient { NoDelay = true };
        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        connectCts.CancelAfter(connectTimeoutMs);
        await tcp.ConnectAsync(host, port, connectCts.Token);

        using var stream = tcp.GetStream();
        stream.ReadTimeout = readTimeoutMs;
        stream.WriteTimeout = readTimeoutMs;

        // Handshake (next state = 1, status)
        await stream.WriteAsync(BuildHandshakePacket(host, port, ProtocolVersionAny), ct);
        // Status Request (empty body)
        await stream.WriteAsync(BuildEmptyPacket(0x00), ct);

        var statusJson = await ReadStatusResponseAsync(stream, ct);

        long? latency = null;
        try
        {
            var nonce = Stopwatch.GetTimestamp();
            await stream.WriteAsync(BuildPingPacket(nonce), ct);
            var sw = Stopwatch.StartNew();
            await ReadPongAsync(stream, ct);
            sw.Stop();
            latency = sw.ElapsedMilliseconds;
        }
        catch
        {
            // Some servers close after status; treat ping as best-effort.
        }

        return ParseStatus(host, port, statusJson, latency);
    }

    private static MinecraftStatusDto ParseStatus(string host, int port, string json, long? latencyMs)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string? versionName = null;
        int? protocol = null;
        if (root.TryGetProperty("version", out var versionEl) && versionEl.ValueKind == JsonValueKind.Object)
        {
            if (versionEl.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
                versionName = n.GetString();
            if (versionEl.TryGetProperty("protocol", out var p) && p.TryGetInt32(out var pv))
                protocol = pv;
        }

        int? playersOnline = null;
        int? playersMax = null;
        List<string>? sample = null;
        if (root.TryGetProperty("players", out var playersEl) && playersEl.ValueKind == JsonValueKind.Object)
        {
            if (playersEl.TryGetProperty("online", out var o) && o.TryGetInt32(out var po)) playersOnline = po;
            if (playersEl.TryGetProperty("max", out var m) && m.TryGetInt32(out var pm)) playersMax = pm;
            if (playersEl.TryGetProperty("sample", out var s) && s.ValueKind == JsonValueKind.Array)
            {
                sample = new List<string>(s.GetArrayLength());
                foreach (var entry in s.EnumerateArray())
                {
                    if (entry.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
                    {
                        var name = nameEl.GetString();
                        if (!string.IsNullOrWhiteSpace(name)) sample.Add(name!);
                    }
                }
            }
        }

        string? motd = null;
        if (root.TryGetProperty("description", out var descEl))
        {
            motd = FlattenChatComponent(descEl);
        }

        string? favicon = null;
        if (root.TryGetProperty("favicon", out var favEl) && favEl.ValueKind == JsonValueKind.String)
        {
            favicon = favEl.GetString();
        }

        return new MinecraftStatusDto(
            host, port, true,
            versionName, protocol,
            playersOnline, playersMax,
            sample,
            motd,
            latencyMs,
            favicon,
            null,
            DateTime.UtcNow);
    }

    private static string? FlattenChatComponent(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.String) return StripFormatting(el.GetString());
        if (el.ValueKind != JsonValueKind.Object) return null;

        var sb = new StringBuilder();
        AppendComponent(el, sb);
        var result = sb.ToString();
        return string.IsNullOrEmpty(result) ? null : StripFormatting(result);
    }

    private static void AppendComponent(JsonElement el, StringBuilder sb)
    {
        if (el.ValueKind == JsonValueKind.String) { sb.Append(el.GetString()); return; }
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray()) AppendComponent(child, sb);
            return;
        }
        if (el.ValueKind != JsonValueKind.Object) return;

        if (el.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
            sb.Append(textEl.GetString());
        if (el.TryGetProperty("extra", out var extraEl) && extraEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in extraEl.EnumerateArray()) AppendComponent(child, sb);
        }
    }

    private static string? StripFormatting(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var sb = new StringBuilder(input.Length);
        for (int i = 0; i < input.Length; i++)
        {
            // Remove legacy section-sign formatting codes (§x).
            if (input[i] == '§' && i + 1 < input.Length) { i++; continue; }
            sb.Append(input[i]);
        }
        return sb.ToString();
    }

    // --- Packet building ---

    private static byte[] BuildHandshakePacket(string host, int port, int protocolVersion)
    {
        using var body = new MemoryStream();
        WriteVarInt(body, 0x00); // packet ID
        WriteVarInt(body, protocolVersion);
        WriteString(body, host);
        Span<byte> portBytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(portBytes, (ushort)port);
        body.Write(portBytes);
        WriteVarInt(body, 1); // next state: status
        return WrapWithLength(body.ToArray());
    }

    private static byte[] BuildEmptyPacket(int packetId)
    {
        using var body = new MemoryStream();
        WriteVarInt(body, packetId);
        return WrapWithLength(body.ToArray());
    }

    private static byte[] BuildPingPacket(long nonce)
    {
        using var body = new MemoryStream();
        WriteVarInt(body, 0x01);
        Span<byte> longBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(longBytes, nonce);
        body.Write(longBytes);
        return WrapWithLength(body.ToArray());
    }

    private static byte[] WrapWithLength(byte[] payload)
    {
        using var ms = new MemoryStream();
        WriteVarInt(ms, payload.Length);
        ms.Write(payload, 0, payload.Length);
        return ms.ToArray();
    }

    // --- Reading ---

    private static async Task<string> ReadStatusResponseAsync(NetworkStream stream, CancellationToken ct)
    {
        var totalLen = await ReadVarIntAsync(stream, ct);
        if (totalLen <= 0 || totalLen > MaxPayloadBytes)
            throw new InvalidDataException($"Bad status packet length: {totalLen}");

        var buffer = new byte[totalLen];
        await ReadExactAsync(stream, buffer, ct);

        var offset = 0;
        var packetId = ReadVarInt(buffer, ref offset);
        if (packetId != 0x00)
            throw new InvalidDataException($"Unexpected status packet id 0x{packetId:X}");

        var jsonLen = ReadVarInt(buffer, ref offset);
        if (jsonLen < 0 || offset + jsonLen > buffer.Length)
            throw new InvalidDataException("Bad JSON length in status response");

        return Encoding.UTF8.GetString(buffer, offset, jsonLen);
    }

    private static async Task ReadPongAsync(NetworkStream stream, CancellationToken ct)
    {
        var totalLen = await ReadVarIntAsync(stream, ct);
        if (totalLen <= 0 || totalLen > 32)
            throw new InvalidDataException($"Bad pong length: {totalLen}");
        var buffer = new byte[totalLen];
        await ReadExactAsync(stream, buffer, ct);
    }

    private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, CancellationToken ct)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(read), ct);
            if (n == 0) throw new EndOfStreamException("Server closed connection mid-read");
            read += n;
        }
    }

    // --- VarInt ---

    private static void WriteVarInt(Stream s, int value)
    {
        var v = (uint)value;
        while (true)
        {
            if ((v & ~0x7Fu) == 0) { s.WriteByte((byte)v); return; }
            s.WriteByte((byte)((v & 0x7F) | 0x80));
            v >>= 7;
        }
    }

    private static void WriteString(Stream s, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteVarInt(s, bytes.Length);
        s.Write(bytes, 0, bytes.Length);
    }

    private static int ReadVarInt(byte[] buffer, ref int offset)
    {
        var result = 0;
        var shift = 0;
        while (true)
        {
            if (offset >= buffer.Length) throw new InvalidDataException("Truncated VarInt");
            var b = buffer[offset++];
            result |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0) return result;
            shift += 7;
            if (shift >= 35) throw new InvalidDataException("VarInt too long");
        }
    }

    private static async Task<int> ReadVarIntAsync(NetworkStream stream, CancellationToken ct)
    {
        var result = 0;
        var shift = 0;
        var oneByte = new byte[1];
        while (true)
        {
            var n = await stream.ReadAsync(oneByte.AsMemory(), ct);
            if (n == 0) throw new EndOfStreamException("Server closed before VarInt");
            var b = oneByte[0];
            result |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0) return result;
            shift += 7;
            if (shift >= 35) throw new InvalidDataException("VarInt too long");
        }
    }

    private static string ShortError(Exception ex) => ex switch
    {
        SocketException se => $"Connection failed ({se.SocketErrorCode})",
        OperationCanceledException => "Timeout",
        EndOfStreamException => "Server closed connection",
        InvalidDataException ide => $"Bad response: {ide.Message}",
        _ => ex.Message,
    };
}
