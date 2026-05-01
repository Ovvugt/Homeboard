using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var dbDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "Homeboard");
Directory.CreateDirectory(dbDir);
var dbPath = Path.Combine(dbDir, "homeboard.db");
var connectionString = $"Data Source={dbPath};Cache=Shared;Foreign Keys=True";

var api = builder.AddProject<Homeboard_API>("API")
    .WithEnvironment("ConnectionStrings__DefaultConnection", connectionString);

builder.AddJavaScriptApp("Frontend", "../../Homeboard.Frontend")
    .WithParentRelationship(api)
    .WaitFor(api);

await builder.Build().RunAsync();
