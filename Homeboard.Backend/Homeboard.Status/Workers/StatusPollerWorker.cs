using Homeboard.Boards.Repositories;
using Homeboard.Status.Repositories;
using Homeboard.Status.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Homeboard.Status.Workers;

public sealed class StatusPollerWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<StatusPollerWorker> logger) : BackgroundService
{
    private static readonly SemaphoreSlim Semaphore = new(8, 8);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSec = config.GetValue<int?>("Status:PollIntervalSeconds") ?? 10;
        var interval = TimeSpan.FromSeconds(intervalSec);

        // Brief delay so the API has time to fully start before we start hitting the network.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Status poll cycle failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task PollOnceAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var tiles = scope.ServiceProvider.GetRequiredService<ITileRepository>();
        var statusRepo = scope.ServiceProvider.GetRequiredService<IStatusRepository>();
        var checker = scope.ServiceProvider.GetRequiredService<IStatusChecker>();

        var allTiles = await tiles.ListAllWithChecksAsync(ct);
        if (allTiles.Count == 0) return;

        var snapshots = (await statusRepo.ListAllAsync(ct)).ToDictionary(s => s.TileId);
        var now = DateTime.UtcNow;

        var due = allTiles.Where(t =>
        {
            if (!snapshots.TryGetValue(t.Id, out var snap)) return true;
            return snap.LastCheckedUtc.AddSeconds(t.StatusInterval) <= now;
        }).ToList();

        if (due.Count == 0) return;

        logger.LogDebug("Polling {Count} tile(s)", due.Count);

        var tasks = due.Select(async tile =>
        {
            await Semaphore.WaitAsync(ct);
            try
            {
                snapshots.TryGetValue(tile.Id, out var prev);
                var snap = await checker.CheckAsync(tile, prev, ct);
                await statusRepo.UpsertAsync(snap, ct);
            }
            finally
            {
                Semaphore.Release();
            }
        });
        await Task.WhenAll(tasks);
    }
}
