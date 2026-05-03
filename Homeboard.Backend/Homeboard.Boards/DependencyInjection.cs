using System.Reflection;
using FluentValidation;
using Homeboard.Boards.Repositories;
using Homeboard.Boards.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Homeboard.Boards;

public static class DependencyInjection
{
    public static IServiceCollection AddBoardsFeature(this IServiceCollection services)
    {
        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<ITileRepository, TileRepository>();
        services.AddScoped<IWidgetRepository, WidgetRepository>();
        services.AddScoped<ISectionRepository, SectionRepository>();
        services.AddScoped<ILayoutRepository, LayoutRepository>();

        services.AddScoped<IBoardReader, BoardReader>();
        services.AddScoped<IBoardCreator, BoardCreator>();
        services.AddScoped<IBoardUpdater, BoardUpdater>();
        services.AddScoped<IBoardDeleter, BoardDeleter>();

        services.AddScoped<ITileCreator, TileCreator>();
        services.AddScoped<ITileUpdater, TileUpdater>();
        services.AddScoped<ITileDeleter, TileDeleter>();

        services.AddScoped<IWidgetCreator, WidgetCreator>();
        services.AddScoped<IWidgetUpdater, WidgetUpdater>();
        services.AddScoped<IWidgetDeleter, WidgetDeleter>();
        services.AddScoped<ILayoutSaver, LayoutSaver>();

        services.AddScoped<ISectionCreator, SectionCreator>();
        services.AddScoped<ISectionUpdater, SectionUpdater>();
        services.AddScoped<ISectionDeleter, SectionDeleter>();

        services.AddScoped<IBoardExporter, BoardExporter>();
        services.AddScoped<IBoardImporter, BoardImporter>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
