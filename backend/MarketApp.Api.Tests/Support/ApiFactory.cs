using System.Data.Common;
using MarketApp.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MarketApp.Api.Tests.Support;

// Runs the real API in memory: CreateClient() sends HTTP requests through the whole pipeline.
// Each factory has its own SQLite in-memory database, migrated at startup by Program.cs
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not Production: that would exit when the mutex exists, open the browser and use C:\ProgramData.
        // Development logs to a path relative to the working directory, which for tests is this project's bin folder
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<MarketAppDbContext>>();

            // Singleton so the connection, and with it the database, lives as long as the factory
            services.AddSingleton<DbConnection>(_ =>
            {
                var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                return connection;
            });
            services.AddDbContext<MarketAppDbContext>((provider, options) =>
                options.UseSqlite(provider.GetRequiredService<DbConnection>()));
        });
    }
}
