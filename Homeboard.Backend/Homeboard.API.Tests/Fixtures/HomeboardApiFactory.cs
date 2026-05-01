using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Homeboard.API.Tests.Fixtures;

public sealed class HomeboardApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"homeboard-test-{Guid.NewGuid():N}.db");

    public string ConnectionString => $"Data Source={_dbPath};Cache=Shared;Foreign Keys=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Status:PollIntervalSeconds"] = "9999",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the status poller hosted service so it doesn't run during tests.
            var pollerType = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();
            foreach (var d in pollerType)
            {
                if (d.ImplementationType?.Name == "StatusPollerWorker")
                {
                    services.Remove(d);
                }
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { /* ignore */ }
    }
}
