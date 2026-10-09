using Microsoft.EntityFrameworkCore;
using Ticketsystem.Data;
using Ticketsystem.Models;

namespace Ticketsystem.Services;

public class TicketService(IDbContextFactory<TicketDbContext> factory) : ITicketService
{
    public const string NummernPraefix = "DEV-";

    public event Action? Geaendert;

    public async Task<List<Ticket>> AlleAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Tickets.AsNoTracking()
            .Include(t => t.Schritte)
            .Include(t => t.Aufgaben)
            .AsSplitQuery()
            .OrderByDescending(t => t.GeaendertAm)
            .ToListAsync();
    }

    public async Task<Ticket?> LadenAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Tickets.AsNoTracking()
            .Include(t => t.Schritte)
            .Include(t => t.Aufgaben)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Ticket> SpeichernAsync(Ticket ticket)
    {
        await using var db = await factory.CreateDbContextAsync();
        ticket.GeaendertAm = DateTime.Now;

        if (ticket.Id == 0)
        {
            ticket.ErstelltAm = DateTime.Now;
            db.Tickets.Add(ticket);
        }
        else
        {
            var vorhanden = await db.Tickets.FirstOrDefaultAsync(t => t.Id == ticket.Id)
                ?? throw new InvalidOperationException("Ticket wurde inzwischen gelöscht.");
            // Nur die Ticketfelder übernehmen – Schritte und Tasks werden separat gespeichert.
            db.Entry(vorhanden).CurrentValues.SetValues(ticket);
        }

        await db.SaveChangesAsync();
        Geaendert?.Invoke();
        return ticket;
    }

    public async Task LoeschenAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        await db.Aufgaben.Where(a => a.TicketId == id).ExecuteDeleteAsync();
        await db.Bilder.Where(b => b.TicketId == id).ExecuteDeleteAsync();
        await db.Schritte.Where(s => s.TicketId == id).ExecuteDeleteAsync();
        await db.Tickets.Where(t => t.Id == id).ExecuteDeleteAsync();
        Geaendert?.Invoke();
    }

    public async Task<bool> NummerVergebenAsync(string nummer, int ausserId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Tickets.AnyAsync(t => t.Ticketnummer == nummer && t.Id != ausserId);
    }

    public async Task<string> NaechsteNummerAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        var nummern = await db.Tickets
            .Where(t => t.Ticketnummer.StartsWith(NummernPraefix))
            .Select(t => t.Ticketnummer)
            .ToListAsync();

        var max = nummern
            .Select(n => int.TryParse(n[NummernPraefix.Length..], out var i) ? i : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{NummernPraefix}{max + 1:0000}";
    }

    public async Task<Vorschlaege> VorschlaegeAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        var t = await db.Tickets.AsNoTracking()
            .Select(x => new { x.Mandant, x.Ansprechpartner, x.Umfeld, x.WartetAuf })
            .ToListAsync();

        static List<string> Distinct(IEnumerable<string?> werte, params string[] standard) =>
            werte.Where(w => !string.IsNullOrWhiteSpace(w)).Select(w => w!.Trim())
                 .Concat(standard)
                 .Distinct(StringComparer.OrdinalIgnoreCase)
                 .Order(StringComparer.CurrentCultureIgnoreCase)
                 .ToList();

        return new Vorschlaege(
            Distinct(t.Select(x => x.Mandant)),
            Distinct(t.Select(x => x.Ansprechpartner)),
            Distinct(t.Select(x => x.Umfeld), "Entwicklung", "Test", "QS", "Produktion"),
            Distinct(t.Select(x => x.WartetAuf)));
    }

    public async Task<Schritt> SchrittHinzufuegenAsync(int ticketId, DateTime datum, string text)
    {
        await using var db = await factory.CreateDbContextAsync();
        var schritt = new Schritt { TicketId = ticketId, Datum = datum, Text = text.Trim() };
        db.Schritte.Add(schritt);
        await db.Tickets.Where(t => t.Id == ticketId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.GeaendertAm, DateTime.Now));
        await db.SaveChangesAsync();
        schritt.Ticket = null;
        Geaendert?.Invoke();
        return schritt;
    }

    public async Task SchrittAendernAsync(int schrittId, DateTime datum, string text)
    {
        await using var db = await factory.CreateDbContextAsync();
        var schritt = await db.Schritte.FirstOrDefaultAsync(s => s.Id == schrittId)
            ?? throw new InvalidOperationException("Schritt wurde inzwischen gelöscht.");
        schritt.Datum = datum;
        schritt.Text = text.Trim();
        await db.Tickets.Where(t => t.Id == schritt.TicketId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.GeaendertAm, DateTime.Now));
        await db.SaveChangesAsync();
        Geaendert?.Invoke();
    }

    public async Task SchrittLoeschenAsync(int schrittId)
    {
        await using var db = await factory.CreateDbContextAsync();
        await db.Schritte.Where(s => s.Id == schrittId).ExecuteDeleteAsync();
        Geaendert?.Invoke();
    }

    // ── Status (z. B. per Drag & Drop im Board) ────────────────
    public async Task StatusAendernAsync(int ticketId, TicketStatus neu)
    {
        await using var db = await factory.CreateDbContextAsync();
        var t = await db.Tickets.FirstOrDefaultAsync(x => x.Id == ticketId)
            ?? throw new InvalidOperationException("Ticket wurde inzwischen gelöscht.");
        var alt = t.Status;
        if (alt == neu) return;

        var jetzt = new DateTime(DateTime.Now.Ticks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute);
        t.Status = neu;
        if (neu == TicketStatus.Wartend && t.WartetSeit is null) t.WartetSeit = jetzt;
        if (neu == TicketStatus.Erledigt)
        {
            t.Erledigt = true;
            t.ErledigtAm ??= jetzt;
        }
        else if (t.Erledigt)
        {
            t.Erledigt = false;
            t.ErledigtAm = null;
        }
        t.GeaendertAm = DateTime.Now;
        db.Schritte.Add(new Schritt { TicketId = ticketId, Datum = DateTime.Now, Text = TicketStatusExtensions.SchrittText(alt, neu) });
        await db.SaveChangesAsync();
        Geaendert?.Invoke();
    }

    // ── Tasks ──────────────────────────────────────────────────
    public async Task<(Aufgabe Aufgabe, bool KeineTaskMehr)> AufgabeHinzufuegenAsync(int ticketId, string titel, AufgabeStatus status = AufgabeStatus.Offen)
    {
        await using var db = await factory.CreateDbContextAsync();
        var max = await db.Aufgaben.Where(a => a.TicketId == ticketId).MaxAsync(a => (int?)a.Reihenfolge) ?? 0;
        var aufgabe = new Aufgabe
        {
            TicketId = ticketId,
            Titel = titel.Trim(),
            Status = status,
            Reihenfolge = max + 1,
            ErstelltAm = DateTime.Now,
            ErledigtAm = status == AufgabeStatus.Erledigt ? DateTime.Now : null,
        };
        db.Aufgaben.Add(aufgabe);
        await db.SaveChangesAsync();
        var keineTaskMehr = await KeineTaskMehrAbgleichenAsync(db, ticketId);
        aufgabe.Ticket = null;
        Geaendert?.Invoke();
        return (aufgabe, keineTaskMehr);
    }

    public async Task<bool> AufgabeAendernAsync(Aufgabe aufgabe)
    {
        await using var db = await factory.CreateDbContextAsync();
        var a = await db.Aufgaben.FirstOrDefaultAsync(x => x.Id == aufgabe.Id)
            ?? throw new InvalidOperationException("Task wurde inzwischen gelöscht.");
        a.Titel = string.IsNullOrWhiteSpace(aufgabe.Titel) ? a.Titel : aufgabe.Titel.Trim();
        a.RestaufwandStunden = aufgabe.RestaufwandStunden is > 0 ? aufgabe.RestaufwandStunden : null;
        a.Reihenfolge = aufgabe.Reihenfolge;
        if (a.Status != aufgabe.Status)
        {
            a.Status = aufgabe.Status;
            a.ErledigtAm = aufgabe.Status == AufgabeStatus.Erledigt ? DateTime.Now : null;
            if (aufgabe.Status == AufgabeStatus.Erledigt) a.RestaufwandStunden = null;   // erledigt = kein Restaufwand
        }
        await db.SaveChangesAsync();
        aufgabe.ErledigtAm = a.ErledigtAm;
        aufgabe.RestaufwandStunden = a.RestaufwandStunden;
        var keineTaskMehr = await KeineTaskMehrAbgleichenAsync(db, a.TicketId);
        Geaendert?.Invoke();
        return keineTaskMehr;
    }

    public async Task<bool> AufgabeLoeschenAsync(int aufgabeId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var a = await db.Aufgaben.FirstOrDefaultAsync(x => x.Id == aufgabeId);
        if (a is null) return false;
        db.Aufgaben.Remove(a);
        await db.SaveChangesAsync();
        var keineTaskMehr = await KeineTaskMehrAbgleichenAsync(db, a.TicketId);
        Geaendert?.Invoke();
        return keineTaskMehr;
    }

    public async Task AufgabenSortierenAsync(int ticketId, IReadOnlyList<int> aufgabenIds)
    {
        await using var db = await factory.CreateDbContextAsync();
        var aufgaben = await db.Aufgaben.Where(a => a.TicketId == ticketId).ToListAsync();
        for (var i = 0; i < aufgabenIds.Count; i++)
        {
            var a = aufgaben.FirstOrDefault(x => x.Id == aufgabenIds[i]);
            if (a is not null) a.Reihenfolge = i + 1;
        }
        await db.SaveChangesAsync();
        Geaendert?.Invoke();
    }

    /// <summary>
    /// „Keine Task mehr“ automatisch: an, wenn alle Tasks erledigt sind; aus, sobald ein Task offen ist.
    /// Ohne Tasks bleibt der Schalter, wie er ist. Liefert den neuen Wert.
    /// </summary>
    private static async Task<bool> KeineTaskMehrAbgleichenAsync(TicketDbContext db, int ticketId)
    {
        var stati = await db.Aufgaben.Where(a => a.TicketId == ticketId).Select(a => a.Status).ToListAsync();
        var ticket = await db.Tickets.FirstAsync(t => t.Id == ticketId);
        if (stati.Count > 0) ticket.KeineTaskMehr = stati.All(s => s == AufgabeStatus.Erledigt);
        ticket.GeaendertAm = DateTime.Now;
        await db.SaveChangesAsync();
        return ticket.KeineTaskMehr;
    }

    // ── Bilder ─────────────────────────────────────────────────
    public async Task<int> BildSpeichernAsync(int ticketId, string dateiname, string contentType, byte[] daten)
    {
        await using var db = await factory.CreateDbContextAsync();
        if (!await db.Tickets.AnyAsync(t => t.Id == ticketId))
            throw new InvalidOperationException("Ticket nicht gefunden.");
        var bild = new Bild
        {
            TicketId = ticketId,
            Dateiname = string.IsNullOrWhiteSpace(dateiname) ? "Bild" : Path.GetFileName(dateiname),
            ContentType = contentType,
            Daten = daten,
            Groesse = daten.LongLength,
            ErstelltAm = DateTime.Now,
        };
        db.Bilder.Add(bild);
        await db.SaveChangesAsync();
        return bild.Id;
    }

    public async Task<Bild?> BildLadenAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Bilder.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
    }
}
