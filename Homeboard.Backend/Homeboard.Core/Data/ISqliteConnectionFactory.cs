using Microsoft.Data.Sqlite;

namespace Homeboard.Core.Data;

public interface ISqliteConnectionFactory
{
    SqliteConnection Create();
}
