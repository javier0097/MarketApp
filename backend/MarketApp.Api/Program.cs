using MarketApp.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("MarketApp");

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<MarketAppDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

var databasePath = Path.GetFullPath(new SqliteConnectionStringBuilder(connectionString).DataSource);
Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<MarketAppDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();
