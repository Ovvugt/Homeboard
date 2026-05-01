using Homeboard.Icons.Repositories;
using Homeboard.Icons.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Homeboard.Icons;

public static class DependencyInjection
{
    public static IServiceCollection AddIconsFeature(this IServiceCollection services)
    {
        services.AddHttpClient("icons", c =>
        {
            c.DefaultRequestHeaders.Add("User-Agent", "Homeboard/0.1 (icon fetch; +https://homeboard.local)");
            c.DefaultRequestHeaders.Add("Accept", "image/*,text/html;q=0.9,*/*;q=0.5");
        });
        services.AddScoped<IIconRepository, IconRepository>();
        services.AddScoped<IIconFetcher, IconFetcher>();
        services.AddScoped<IIconResolver, IconResolver>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
