using Homeboard.Status.Repositories;
using Homeboard.Status.Services;
using Homeboard.Status.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Homeboard.Status;

public static class DependencyInjection
{
    public static IServiceCollection AddStatusFeature(this IServiceCollection services)
    {
        services.AddHttpClient("status", c =>
        {
            c.DefaultRequestHeaders.Add("User-Agent", "Homeboard/0.1 (status check)");
        });
        services.AddScoped<IStatusRepository, StatusRepository>();
        services.AddScoped<IStatusChecker, StatusChecker>();
        services.AddScoped<IStatusReader, StatusReader>();
        services.AddScoped<IStatusForcer, StatusForcer>();
        services.AddHostedService<StatusPollerWorker>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
