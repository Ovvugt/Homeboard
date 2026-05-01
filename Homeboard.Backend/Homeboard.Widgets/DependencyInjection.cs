using Homeboard.Widgets.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Homeboard.Widgets;

public static class DependencyInjection
{
    public static IServiceCollection AddWidgetsFeature(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpClient("openmeteo");
        services.AddScoped<IWeatherFetcher, WeatherFetcher>();
        return services;
    }
}
