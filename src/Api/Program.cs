using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Omne.Screen.Api.Data;
using Omne.Screen.Api.Inventory;

var builder = WebApplication.CreateBuilder(args);

// Connection string comes from config/env: ConnectionStrings__OmneScreen.
// Never hard-code credentials — the compose file / secret store supplies this.
var connectionString = builder.Configuration.GetConnectionString("OmneScreen")
    ?? throw new InvalidOperationException("ConnectionStrings__OmneScreen must be configured.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

// Create the schema + seed on startup. Kept simple for the slice; in the real
// world this is a separate migrations step (see the longer exercise).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Database not reachable at startup; /health will report unhealthy.");
    }
}

// Liveness/readiness: pings the database. A meaningful health check is part of
// the brief — feel free to split liveness vs. readiness.
app.MapGet("/health", async (AppDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new { status = "healthy" })
        : Results.Json(new { status = "unhealthy", reason = "database unreachable" }, statusCode: 503);
});

app.MapGet("/api/widgets", async (AppDbContext db) =>
    await db.Widgets.OrderBy(w => w.Id).ToListAsync());

// Uses Newtonsoft.Json on purpose — exercises the dependency end to end.
app.MapGet("/api/widgets/stats", async (AppDbContext db) =>
{
    var widgets = await db.Widgets.ToListAsync();
    var stats = new
    {
        count = widgets.Count,
        totalQuantity = InventoryMath.TotalQuantity(widgets.Select(w => w.Quantity)),
    };
    return Results.Content(JsonConvert.SerializeObject(stats), "application/json");
});

app.Run();

// Exposed so WebApplicationFactory-style integration tests can reference the entry point.
public partial class Program;
