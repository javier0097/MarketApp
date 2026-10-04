using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MarketApp.Api.Data;

public static class DatabaseStartup
{
    public static void MigrateDatabase(this WebApplication app)
    {
        var connectionString = app.Configuration.GetConnectionString("MarketApp");
        var databasePath = Path.GetFullPath(new SqliteConnectionStringBuilder(connectionString).DataSource);
        var databaseFolder = Path.GetDirectoryName(databasePath)!;
        Directory.CreateDirectory(databaseFolder);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketAppDbContext>();

        if (app.Environment.IsProduction() && File.Exists(databasePath) && db.Database.GetPendingMigrations().Any())
        {
            var backupPath = BackUp(connectionString, databaseFolder);
            app.Logger.LogInformation("Database backed up to {BackupPath} before applying migrations", backupPath);
        }

        db.Database.Migrate();
    }

    // SQLite's backup API also copies recent changes still held in the -wal file, which a plain file copy would miss
    private static string BackUp(string? connectionString, string databaseFolder)
    {
        var backupFolder = Path.Combine(databaseFolder, "backups");
        Directory.CreateDirectory(backupFolder);
        var backupPath = Path.Combine(backupFolder, $"marketapp-{DateTime.Now:yyyyMMdd-HHmmss}.db");

        using var source = new SqliteConnection(connectionString);
        using var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = backupPath, Pooling = false }.ToString());
        source.Open();
        source.BackupDatabase(target);

        return backupPath;
    }
}
