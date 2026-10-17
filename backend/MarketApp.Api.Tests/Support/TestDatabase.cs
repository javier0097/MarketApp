using MarketApp.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MarketApp.Api.Tests.Support;

// SQLite in memory, the same engine as production. The database only exists while
// its connection is open, so the connection stays open until the test disposes this
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    public TestDatabase()
    {
        connection.Open();
        using var db = CreateContext();
        db.Database.Migrate();
    }

    // A new context per call, like each request in the app: a test can save with one
    // and check with another, so it reads what reached the database
    public MarketAppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<MarketAppDbContext>().UseSqlite(connection).Options);

    public void Dispose() => connection.Dispose();
}
