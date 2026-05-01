using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Homeboard.Core.Data;

public sealed class SqliteConnectionFactory(IConfiguration config) : ISqliteConnectionFactory
{
    private readonly string _connectionString =
        config.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

    public SqliteConnection Create() => new(_connectionString);
}
