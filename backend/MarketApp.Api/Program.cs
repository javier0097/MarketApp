using MarketApp.Api.Data;
using MarketApp.Api.Exceptions;
using MarketApp.Api.Services;
using MarketApp.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var appUrl = builder.Configuration["Urls"]!;

// "using" keeps the mutex alive until the app exits; without it the GC could release it early
using var instanceLock = new Mutex(true, "MarketApp", out var isFirstInstance);
if (builder.Environment.IsProduction() && !isFirstInstance)
{
    OpenBrowser(appUrl);
    return;
}

builder.Services.AddSerilog(configuration => configuration.ReadFrom.Configuration(builder.Configuration));

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<MarketAppDbContext>(options => options
    .UseSqlite(builder.Configuration.GetConnectionString("MarketApp"))
    .EnableSensitiveDataLogging());
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.MigrateDatabase();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();

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
