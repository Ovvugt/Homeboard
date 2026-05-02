using System.Text.Json.Serialization;
using DotNetEnv;
using Homeboard.Boards;
using Homeboard.Core;
using Homeboard.Core.Data;
using Homeboard.Icons;
using Homeboard.Status;
using Homeboard.Widgets;
using Serilog;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, _, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext());

builder.Services.AddCore(builder.Configuration);
builder.Services.AddBoardsFeature();
builder.Services.AddStatusFeature();
builder.Services.AddWidgetsFeature();
builder.Services.AddIconsFeature();
builder.Services.AddControllers().AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Services.GetRequiredService<DbInitializer>().Run();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

namespace Homeboard.API
{
    public partial class Program;
}
