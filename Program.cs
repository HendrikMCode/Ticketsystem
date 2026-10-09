using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Ticketsystem.Components;
using Ticketsystem.Data;
using Ticketsystem.Services;

var kultur = new CultureInfo("de-DE");
CultureInfo.DefaultThreadCurrentCulture = kultur;
CultureInfo.DefaultThreadCurrentUICulture = kultur;

var builder = WebApplication.CreateBuilder(args);

// ── Datenbank: SQLite-Datei im Ordner "Daten" neben der App ────────────
var datenOrdner = Path.Combine(builder.Environment.ContentRootPath, "Daten");
Directory.CreateDirectory(datenOrdner);
var dbPfad = Path.Combine(datenOrdner, "tickets.db");

// Schlüssel für Formulare/Sitzungen dauerhaft speichern (wichtig im Docker-Container,
// sonst sind nach jedem Neustart offene Browser-Tabs ungültig)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(datenOrdner, "Schluessel")));

builder.Services.AddDbContextFactory<TicketDbContext>(o => o.UseSqlite($"Data Source={dbPfad}"));
builder.Services.AddSingleton<ITicketService, TicketService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Datenbank beim Start anlegen bzw. aktualisieren
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TicketDbContext>>();
    using var db = factory.CreateDbContext();
    // legt die Datenbank an bzw. ergänzt neue Tabellen – vorhandene Tickets bleiben erhalten
    DatenbankAktualisierung.Ausfuehren(db, datenOrdner, app.Logger);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

// ── Bilder: Hochladen (aus dem Browser per Strg+V / Drag & Drop) und Anzeigen ──
const int MaxBildBytes = 20 * 1024 * 1024;
app.MapPost("/api/bilder", async (HttpRequest request, int ticket, string? name, ITicketService service) =>
{
    var contentType = request.ContentType?.Split(';')[0].Trim() ?? "";
    if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) || contentType.Contains("svg"))
        return Results.BadRequest("Nur Bilder (PNG, JPG, GIF, WebP) sind erlaubt.");

    using var puffer = new MemoryStream();
    await request.Body.CopyToAsync(puffer);
    if (puffer.Length == 0) return Results.BadRequest("Leere Datei.");
    if (puffer.Length > MaxBildBytes) return Results.BadRequest("Das Bild ist größer als 20 MB.");

    var id = await service.BildSpeichernAsync(ticket, name ?? "Bild", contentType, puffer.ToArray());
    return Results.Ok(new { id, url = $"bilder/{id}" });
}).DisableAntiforgery();

app.MapGet("/bilder/{id:int}", async (int id, ITicketService service, HttpResponse response) =>
{
    var bild = await service.BildLadenAsync(id);
    if (bild is null) return Results.NotFound();
    response.Headers.CacheControl = "private, max-age=31536000, immutable";
    return Results.File(bild.Daten, bild.ContentType, enableRangeProcessing: false);
});
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Browser automatisch öffnen (abschaltbar in appsettings.json: "BrowserOeffnen": false)
if (app.Configuration.GetValue("BrowserOeffnen", true))
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var url = app.Urls.FirstOrDefault() ?? "http://localhost:5080";
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* kein Browser verfügbar – egal */ }
        Console.WriteLine($"\n  Ticketsystem läuft auf {url}  (Beenden mit Strg+C)\n  Datenbank: {dbPfad}\n");
    });
}

app.Run();
