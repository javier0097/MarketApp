using System.Diagnostics;
using MarketApp.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var appUrl = builder.Configuration["Urls"]!;

// "using" keeps the mutex alive until the app exits; without it the GC could release it early
using var instanceLock = new Mutex(true, "MarketApp", out var isFirstInstance);
if (builder.Environment.IsProduction() && !isFirstInstance)
{
    OpenBrowser(appUrl);
    return;
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<MarketAppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("MarketApp")));

var app = builder.Build();

app.MigrateDatabase();

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

if (app.Environment.IsProduction())
{
    app.Lifetime.ApplicationStarted.Register(() => OpenBrowser(appUrl));
}

app.Run();

static void OpenBrowser(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
