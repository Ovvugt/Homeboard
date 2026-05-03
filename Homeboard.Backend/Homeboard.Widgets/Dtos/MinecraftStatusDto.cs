namespace Homeboard.Widgets.Dtos;

public sealed record MinecraftStatusDto(
    string Host,
    int Port,
    bool Online,
    string? VersionName,
    int? ProtocolVersion,
    int? PlayersOnline,
    int? PlayersMax,
    IReadOnlyList<string>? PlayerSample,
    string? Motd,
    long? LatencyMs,
    string? FaviconDataUri,
    string? Error,
    DateTime FetchedUtc);
